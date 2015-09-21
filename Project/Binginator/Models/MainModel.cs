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
using OpenQA.Selenium.Support.UI;

namespace Binginator.Models {
    public class MainModel {
        private IWebDriver _driver = null;
        private WebDriverWait _wait;
        private MainViewModel _viewModel;
        private List<string> _searches;

        public void Launch(bool mobile) {
            Quit();

            ChromeDriverService service = ChromeDriverService.CreateDefaultService(App.Folder);
            service.HideCommandPromptWindow = true;

            ChromeOptions options = new ChromeOptions();
            options.AddArgument("start-maximized");
            options.AddArgument("user-data-dir=" + App.Folder + "profile");

            if (mobile)
                options.AddAdditionalCapability("mobileEmulation", new Dictionary<string, string> { { "deviceName", "Google Nexus 5" } });

            _driver = new ChromeDriver(service, options);
            _driver.Manage().Timeouts().ImplicitlyWait(TimeSpan.FromSeconds(10));

            _wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(3));

            LogUpdate("Launching Chrome " + (mobile ? "Mobile" : "Desktop"), Colors.Blue);
            LogUpdate(_driver.CurrentWindowHandle, Colors.Silver);

            _driver.Navigate().GoToUrl("https://www.bing.com/");
        }

        private void LogUpdate(string data, Color color, bool inline = false) {
            _viewModel.LogUpdate(data, color, inline);
        }

        internal void Prepare() {
            _searches = new List<string>();
        }

        internal void SetViewModel(MainViewModel viewModel) {
            _viewModel = viewModel;
        }

        internal void Quit() {
            if (_driver != null) {
                try {
                    var windows = _driver.WindowHandles;
                    if (windows.Count > 0) {
                        LogUpdate("close opened tabs", Colors.Black);
                        for (int i = 0; i < windows.Count; i++) {
                            _driver.SwitchTo().Window(windows[i]);
                            LogUpdate(_driver.CurrentWindowHandle, Colors.Silver);
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

        public async Task SearchMobile() {
            uint NeededSearches = _viewModel.MobileSearches;
            Random r = new Random();

            Launch(true);

            await Task.Delay(1000);
            IWebElement rewardsId = _getElementWait(By.Id("id_rwds_b"));

            if (rewardsId == null)
                LogUpdate("timeout :(", Colors.Red);
            else if (rewardsId.GetAttribute("href") == "https://www.bing.com/rewards/dashboard") {
                LogUpdate("logged in", Colors.Blue);

                IWebElement ul = _getElement(By.Id("hc_popnow"));

                if (ul == null)
                    LogUpdate("unable to find ul", Colors.Red);
                else {
                    LogUpdate("opening new tabs for popular news topics", Colors.Blue);
                    Actions builder = new Actions(_driver);
                    uint count = 0;

                    foreach (IWebElement aTag in _getElements(By.TagName("a"), ul)) {
                        string href = aTag.GetAttribute("href");
                        if (href.EndsWith("FORM=HPNN01")) {
                            href = href.Substring(0, href.IndexOf("&"));
                            if (!_searches.Contains(href)) {
                                LogUpdate("(" + count + ") open tab " + href, Colors.Black);

                                builder.KeyDown(Keys.Control).Click(aTag).KeyUp(Keys.Control).Build().Perform();

                                _searches.Add(href);
                                count++;

                                if (count == NeededSearches)
                                    break;

                                await Task.Delay(r.Next(1000, 5000));
                            }
                        }
                    }

                    while (count < NeededSearches) {
                        LogUpdate("still not there, opening more", Colors.Orange);

                        var windows = _driver.WindowHandles;
                        if (windows.Count == 1) {
                            LogUpdate("ran out of windows to search with", Colors.Red);
                            break;
                        }

                        for (int i = 1; i < windows.Count; i++) {
                            _driver.SwitchTo().Window(windows[i]);
                            LogUpdate(_driver.CurrentWindowHandle, Colors.Silver);

                            if (!_driver.Title.EndsWith(" - Bing")) {
                                _driver.Close();
                                continue;
                            }

                            int rand = r.Next(3);
                            if (rand == 0) {
                                IWebElement el = _getElement(By.Id("elst"));

                                if (el == null)
                                    LogUpdate("unable to find elst", Colors.Red);
                                else {
                                    el = _getElement(By.TagName("a"), el);
                                    if (el == null)
                                        LogUpdate("unable to find a", Colors.Red);
                                    else {
                                        string href = el.GetAttribute("href");
                                        if (!_searches.Contains(href)) {
                                            LogUpdate("open new random " + href, Colors.Black);

                                            builder.KeyDown(Keys.Control).Click(el).KeyUp(Keys.Control).Build().Perform();

                                            _searches.Add(href);
                                        }
                                    }
                                }
                            }

                            IWebElement ol = _getElement(By.ClassName("b_vlist2col"));
                            bool hadRelated = false;

                            if (ol == null)
                                LogUpdate("unable to find b_vlist2col", Colors.Red);
                            else {
                                foreach (IWebElement aTag in _getElements(By.TagName("a"), ol)) {
                                    string href = aTag.GetAttribute("href");
                                    if (href.Contains("&FORM=QSRE")) {
                                        href = href.Substring(0, href.IndexOf("&"));
                                        if (!_searches.Contains(href)) {
                                            LogUpdate("(" + count + ") click extra " + href, Colors.Black);

                                            aTag.Click();

                                            hadRelated = true;
                                            _searches.Add(href);
                                            count++;
                                            break;
                                        }
                                    }
                                }
                            }

                            if (!hadRelated) {
                                LogUpdate("no related searches; closing tab", Colors.Green);
                                _driver.Close();
                            }

                            if (count == NeededSearches)
                                break;

                            await Task.Delay(r.Next(1000, 5000));
                        }
                    }
                }

                await Task.Delay(1000);
            }
            else
                LogUpdate("log in plz", Colors.Red);

            Quit();
        }

        internal void ResetProfile() {
            Quit();

            Directory.Delete("profile", true);
        }

        public async Task SearchDesktop() {
            uint NeededSearches = _viewModel.DesktopSearches;
            Random r = new Random();

            Launch(false);

            await Task.Delay(1000);
            IWebElement name = _getElementWait(By.Id("id_n"));

            if (name == null)
                LogUpdate("timeout :(", Colors.Red);
            else if (name.Displayed) {
                LogUpdate("logged in", Colors.Blue);
                IWebElement ul = _getElement(By.Id("crs_pane"));

                if (ul == null)
                    LogUpdate("unable to find ul", Colors.Red);
                else {
                    LogUpdate("opening new tabs for popular news topics", Colors.Blue);
                    Actions builder = new Actions(_driver);
                    uint count = 0;

                    foreach (IWebElement aTag in _getElements(By.TagName("a"), ul)) {
                        string href = aTag.GetAttribute("href");
                        if (href.EndsWith("FORM=HPNN01")) {
                            href = href.Substring(0, href.IndexOf("&"));
                            if (!_searches.Contains(href)) {
                                LogUpdate("(" + count + ") open tab " + href, Colors.Black);

                                builder.KeyDown(Keys.Control).Click(aTag).KeyUp(Keys.Control).Build().Perform();

                                _searches.Add(href);
                                count++;

                                if (count == NeededSearches)
                                    break;

                                //if (count == 6)     // temp
                                //    break;          // temp

                                await Task.Delay(r.Next(1000, 5000));
                            }
                        }
                    }

                    bool forcedOnce = true;
                    while (count < NeededSearches || forcedOnce) {
                        forcedOnce = false;
                        LogUpdate("still not there, opening more", Colors.Orange);

                        var windows = _driver.WindowHandles;
                        if (windows.Count == 1) {
                            LogUpdate("ran out of windows to search with", Colors.Red);
                            break;
                        }

                        for (int i = 1; i < windows.Count; i++) {
                            _driver.SwitchTo().Window(windows[i]);
                            LogUpdate(_driver.CurrentWindowHandle, Colors.Silver);

                            if (!_driver.Title.EndsWith(" - Bing")) {
                                _driver.Close();
                                continue;
                            }

                            int rand = r.Next(3);
                            if (rand == 0) {
                                IWebElement el = _getElement(By.ClassName("mcd"));

                                if (el == null)
                                    LogUpdate("unable to find mcd", Colors.Red);
                                else {
                                    el = _getElement(By.TagName("a"), el);
                                    if (el == null)
                                        LogUpdate("unable to find a", Colors.Red);
                                    else {
                                        string href = el.GetAttribute("href");
                                        LogUpdate("open new random " + href, Colors.Black);
                                        builder.KeyDown(Keys.Control).Click(el).KeyUp(Keys.Control).Build().Perform();
                                    }
                                }
                            }

                            if (count < NeededSearches) { // first time here, might already have the amount
                                IWebElement ol = _getElement(By.Id("b_context"));
                                bool hadRelated = false;

                                if (ol == null)
                                    LogUpdate("unable to find ol", Colors.Red);
                                else {
                                    foreach (IWebElement aTag in _getElements(By.TagName("a"), ol)) {
                                        string href = aTag.GetAttribute("href");
                                        if (href.Contains("&FORM=R5FD")) {
                                            href = href.Substring(0, href.IndexOf("&"));
                                            if (!_searches.Contains(href)) {
                                                LogUpdate("(" + count + ") click extra " + href, Colors.Black);

                                                aTag.Click();

                                                hadRelated = true;
                                                _searches.Add(href);
                                                count++;
                                                break;
                                            }
                                        }
                                    }
                                }

                                if (!hadRelated) {
                                    LogUpdate("no related searches; closing tab", Colors.Green);
                                    _driver.Close();
                                }

                                if (count == NeededSearches)
                                    break;
                            }

                            await Task.Delay(r.Next(1000, 5000));
                        }
                    }
                }

                await Task.Delay(1000);
            }
            else
                LogUpdate("log in plz", Colors.Red);

            Quit();
        }

        private IWebElement _getElement(By by, IWebElement context = null) {
            LogUpdate("_getElement " + by, Colors.Purple);
            try {
                if (context != null)
                    return context.FindElement(by);
                else
                    return _driver.FindElement(by);
            }
            catch (NoSuchElementException) {
                return null;
            }
        }

        private ReadOnlyCollection<IWebElement> _getElements(By by, IWebElement context = null) {
            LogUpdate("_getElements " + by, Colors.Purple);
            try {
                if (context != null)
                    return context.FindElements(by);
                else
                    return _driver.FindElements(by);
            }
            catch (NoSuchElementException) {
                return null;
            }
        }

        private IWebElement _getElementWait(By by) {
            LogUpdate("_getElementWait " + by, Colors.Purple);
            try {
                return _wait.Until<IWebElement>((d) => {
                    try {
                        return d.FindElement(by);
                    }
                    catch (NoSuchElementException) { return null; }
                });
            }
            catch (WebDriverTimeoutException) {
                return null;
            }
        }
    }
}
