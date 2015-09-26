using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media;
using Binginator.Windows.ViewModels;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Interactions;

namespace Binginator.Models {
    public class MainModel {
        private IWebDriver _driver = null;
        private Actions _builder;
        private MainViewModel _viewModel;
        private List<string> _searches;
        private uint _neededSearches;
        private int _count;
        private Random _random;
        private bool _cancel;

        internal void SetViewModel(MainViewModel viewModel) {
            _viewModel = viewModel;
        }

        private void LogUpdate(string data, Color color, bool inline = false) {
            _viewModel.LogUpdate(data, color, inline);
        }

        internal void Launch(bool mobile, string url = "https://www.bing.com/") {
            Quit();

            ChromeDriverService service = ChromeDriverService.CreateDefaultService(App.Folder);
            service.HideCommandPromptWindow = true;

            ChromeOptions options = new ChromeOptions();
            options.AddArgument("start-maximized");
            options.AddArgument("user-data-dir=" + App.Folder + "profile");

            if (mobile)
                options.AddAdditionalCapability("mobileEmulation", new Dictionary<string, string> { { "deviceName", "Google Nexus 5" } });

            _driver = new ChromeDriver(service, options);
            _driver.Manage().Timeouts().ImplicitlyWait(TimeSpan.FromSeconds(3));

            _builder = new Actions(_driver);

            LogUpdate("Launching Chrome " + (mobile ? "Mobile" : "Desktop"), Colors.Blue);

            if (url != null)
                _driver.Navigate().GoToUrl(url);
        }


        internal void ResetProfile() {
            Quit();

            Directory.Delete("profile", true);
            LogUpdate("deleted chrome profile", Colors.Blue);
        }

        internal async void Search() {
            _searches = new List<string>();
            _random = new Random();
            _cancel = false;

            try {
                await _offers();
            }
            catch (Exception ex) {
                LogUpdate("_offers() - CHROMEDRIVER crashed " + ex.Message, Colors.Red);
            }

            try {
                await _searchMobile();
            }
            catch (Exception ex) {
                LogUpdate("_searchMobile() - CHROMEDRIVER crashed " + ex.Message, Colors.Red);
            }

            try {
                await _searchDesktop();
            }
            catch (Exception ex) {
                LogUpdate("_searchDesktop() - CHROMEDRIVER crashed " + ex.Message, Colors.Red);
            }
        }

        internal void Quit(bool cancel = false) {
            if (cancel)
                _cancel = true;

            if (_driver != null) {
                try {
                    var windows = _driver.WindowHandles;
                    if (windows.Count > 0) {
                        LogUpdate("close opened tabs", Colors.SlateBlue);
                        for (int i = 0; i < windows.Count; i++) {
                            _driver.SwitchTo().Window(windows[i]);
                            _driver.Close();
                        }
                    }
                }
                catch (InvalidOperationException) {
                    LogUpdate("where did chrome go?", Colors.Red);
                }

                _driver.Quit();
                _driver = null;
            }
        }

        private async Task _searchMobile() {
            if (_cancel)
                return;

            _neededSearches = _viewModel.MobileCredits * 2;
            _count = 0;

            Launch(true);

            await _search(
                "//div[@id='hc_popnow']//a[@href[contains(.,'&FORM=HPNN01')]]",
                "//div[@id='elst']//h2/a",
                "//ol[@id='b_results']//div[@class='b_rs']/h2[text()='Related searches']/following-sibling::*//a[@href[contains(.,'&FORM=QSRE')]]"
                );

            Quit();
        }



        private async Task _searchDesktop() {
            if (_cancel)
                return;

            _neededSearches = _viewModel.DesktopCredits * 2;
            _count = 0;

            Launch(false);

            await _search(
                "//ul[@id='crs_pane']//a[@href[contains(.,'&FORM=HPNN01')]]",
                "//div[contains(concat(' ',@class,' '),' mcd ')]//h4/a",
                "//ol[@id='b_context']//h2[text()='Related searches']/following-sibling::ul//a[@href[contains(.,'&FORM=R5FD')]]"
                );

            Quit();
        }

        private async Task _search(string news, string random, string related) {
            if (_neededSearches == 0 || _neededSearches > 100) {
                LogUpdate("needed searches wrong", Colors.Red);
                return;
            }

            LogUpdate("opening new tabs for popular news topics", Colors.Blue);
            await Task.Delay(1000);

            await _openNews(news);
            if (_count < _neededSearches && _count > 12) {
                LogUpdate("check news topics again", Colors.Orange);
                await _openNews(news);
            }

            if (_count > 0)
                do {
                    LogUpdate("inside random/extra loop", Colors.Orange);

                    var windows = _driver.WindowHandles;
                    if (windows.Count == 1) {
                        LogUpdate("ran out of tabs to search with", Colors.Red);
                        break;
                    }

                    for (int i = 1; i < windows.Count; i++) {
                        _driver.SwitchTo().Window(windows[i]);

                        if (!_driver.Title.EndsWith(" - Bing")) {
                            _driver.Close();
                            continue;
                        }

                        if (_random.Next(4) == 0) {
                            if (_openRandom(random))
                                await Task.Delay(_random.Next(1000, 5000));
                            else
                                LogUpdate("unable to locate a random result", Colors.Red);
                        }

                        if (_count < _neededSearches) { // first time here, might already have the amount
                            if (_openRelated(related))
                                await Task.Delay(_random.Next(1000, 5000));
                            else {
                                LogUpdate("no related searches; closing tab", Colors.Green);
                                _driver.Close();
                            }
                        }
                    }
                } while (_count < _neededSearches);
        }

        private async Task _openNews(string xpath) {
            var elements = _getElements(By.XPath(xpath));
            if (elements.Count == 0)
                LogUpdate("unable to find any", Colors.Red);
            else {
                foreach (IWebElement element in elements) {
                    string href = element.GetAttribute("href");
                    href = href.Substring(0, href.IndexOf("&"));

                    if (!_searches.Contains(href)) {
                        _searches.Add(href);
                        _count++;
                        LogUpdate("(" + _count + ") load " + href, Colors.Black);

                        _builder.KeyDown(Keys.Control).Click(element).KeyUp(Keys.Control).Build().Perform();

                        if (_count >= _neededSearches)
                            break;

                        await Task.Delay(_random.Next(1000, 5000));
                    }
                    else
                        LogUpdate("already loaded " + href, Colors.LightGray);
                }
            }
        }


        private bool _openRandom(string xpath) {
            var elements = _getElements(By.XPath(xpath));
            if (elements.Count == 0)
                return false;

            IWebElement element = elements[_random.Next(0, elements.Count)];
            LogUpdate("open random result " + element.GetAttribute("href"), Colors.Black);
            _builder.KeyDown(Keys.Control).Click(element).KeyUp(Keys.Control).Build().Perform();

            return true;
        }

        private bool _openRelated(string xpath) {
            var elements = _getElements(By.XPath(xpath));
            if (elements.Count > 0) {
                foreach (IWebElement element in elements) {
                    string href = element.GetAttribute("href");
                    href = href.Substring(0, href.IndexOf("&"));
                    if (!_searches.Contains(href)) {
                        _searches.Add(href);
                        _count++;
                        LogUpdate("(" + _count + ") load " + href, Colors.Black);

                        element.Click();
                        return true;
                    }
                    else
                        LogUpdate("already loaded " + href, Colors.LightGray);
                }
            }

            return false;
        }

        private async Task _offers() {
            if (_cancel)
                return;

            Launch(false, "https://www.bing.com/rewards/dashboard");

            LogUpdate("checking for offers", Colors.Blue);
            await Task.Delay(1000);

            if (_getElement(By.XPath("//div[@class='simpleSignIn']")) != null) {
                LogUpdate("SIGN IN TO BING", Colors.Red);
                _cancel = true;
            }
            else {
                List<string> offers = new List<string>();

                while (true) {
                    IWebElement element = _getElement(By.XPath("//div[@class='dashboard-title'][text()='Earn and explore']/following-sibling::ul//div[contains(concat(' ',@class,' '),' open-check ')]/ancestor::a[@href[contains(.,'/rewardsapp/redirect?')]]"));
                    if (element == null) {
                        LogUpdate("unable to find any", Colors.Red);
                        break;
                    }
                    else {
                        IWebElement title = _getElement(By.XPath("span/span[@class='title']"), element);
                        if (title == null)
                            title = element;

                        if (!offers.Contains(title.Text)) {
                            offers.Add(title.Text);
                            LogUpdate("load offer " + title.Text, Colors.Black);

                            element.Click();
                        }
                        else
                            LogUpdate("already loaded " + title.Text, Colors.LightGray);

                        await Task.Delay(_random.Next(1000, 5000));
                    }
                }
            }

            Quit();
        }

        private IWebElement _getElement(By by, IWebElement context = null) {
            LogUpdate("_getElement " + by, Colors.Purple);
            try {
                if (context != null)
                    return context.FindElement(by);

                return _driver.FindElement(by);
            }
            catch (NoSuchElementException) {
                return null;
            }
        }

        private ReadOnlyCollection<IWebElement> _getElements(By by, IWebElement context = null) {
            LogUpdate("_getElements " + by, Colors.Purple);
            if (context != null)
                return context.FindElements(by);

            return _driver.FindElements(by);
        }
    }
}
