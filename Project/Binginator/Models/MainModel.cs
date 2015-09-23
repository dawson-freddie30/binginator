﻿using System;
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
        private Actions _builder;
        private MainViewModel _viewModel;
        private List<string> _searches;
        private uint _neededSearches;
        private int _count;
        private Random _random;

        internal void SetViewModel(MainViewModel viewModel) {
            _viewModel = viewModel;
        }

        private void LogUpdate(string data, Color color, bool inline = false) {
            _viewModel.LogUpdate(data, color, inline);
        }

        internal void Launch(bool mobile) {
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

            _wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(3));
            _builder = new Actions(_driver);

            LogUpdate("Launching Chrome " + (mobile ? "Mobile" : "Desktop"), Colors.Blue);

            _driver.Navigate().GoToUrl("https://www.bing.com/");
        }


        internal void ResetProfile() {
            Quit();

            Directory.Delete("profile", true);
        }

        internal void Quit() {
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

        internal void Prepare() {
            _searches = new List<string>();
            _random = new Random();
        }

        public async Task SearchMobile() {
            _neededSearches = _viewModel.MobileSearches;
            _count = 0;

            Launch(true);

            await Task.Delay(1000);
            IWebElement rewardsId = _getElementWait(By.Id("id_rwds_b"));

            if (rewardsId == null)
                LogUpdate("timeout :(", Colors.Red);
            else if (rewardsId.GetAttribute("href") == "https://www.bing.com/rewards/dashboard") {
                await _search(
                    "//div[@id='hc_popnow']//a[@href[contains(.,'&FORM=HPNN01')]]",
                    "//div[@id='elst']//h2/a",
                    "//ol[@id='b_results']//div[@class='b_rs']/h2[text()='Related searches']/following-sibling::*//a[@href[contains(.,'&FORM=QSRE')]]"
                    );
            }
            else
                LogUpdate("log in plz", Colors.Red);

            Quit();
        }



        public async Task SearchDesktop() {
            _neededSearches = _viewModel.DesktopSearches;
            _count = 0;

            Launch(false);

            await Task.Delay(1000);
            IWebElement name = _getElementWait(By.Id("id_n"));

            if (name == null)
                LogUpdate("timeout :(", Colors.Red);
            else if (name.Displayed) {
                await _search(
                    "//ul[@id='crs_pane']//a[@href[contains(.,'&FORM=HPNN01')]]",
                    "//div[contains(concat(' ',@class,' '),' mcd ')]//h4/a",
                    "//ol[@id='b_context']//h2[text()='Related searches']/following-sibling::ul//a[@href[contains(.,'&FORM=R5FD')]]"
                    );
            }
            else
                LogUpdate("log in plz", Colors.Red);

            Quit();
        }

        private async Task _search(string news, string random, string related) {
            LogUpdate("opening new tabs for popular news topics", Colors.Blue);

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
                            _openRandom(random);
                        }

                        if (_count < _neededSearches) { // first time here, might already have the amount
                            _openRelated(related);
                        }

                        await Task.Delay(_random.Next(1000, 5000));
                    }
                } while (_count < _neededSearches);

            await Task.Delay(1000);
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
#if DEBUG
                    else
                        LogUpdate("already loaded " + href, Colors.LightGray);
#endif
                }
            }
        }

        private void _openRandom(string xpath) {
            var elements = _getElements(By.XPath(xpath));
            if (elements.Count == 0)
                LogUpdate("unable to locate a random result", Colors.Red);
            else {
                IWebElement element = elements[_random.Next(0, elements.Count)];
                LogUpdate("open random result " + element.GetAttribute("href"), Colors.Black);
                _builder.KeyDown(Keys.Control).Click(element).KeyUp(Keys.Control).Build().Perform();
            }
        }

        private void _openRelated(string xpath) {
            var elements = _getElements(By.XPath(xpath));
            if (elements.Count == 0) {
                LogUpdate("no related searches; closing tab", Colors.Green);
                _driver.Close();
            }
            else {
                foreach (IWebElement element in elements) {
                    string href = element.GetAttribute("href");
                    href = href.Substring(0, href.IndexOf("&"));
                    if (!_searches.Contains(href)) {
                        _searches.Add(href);
                        _count++;
                        LogUpdate("(" + _count + ") load " + href, Colors.Black);

                        element.Click();
                        break;
                    }
                    else
                        LogUpdate("already loaded " + href, Colors.LightGray);
                }
            }

        }

        private IWebElement _getElement(By by) {
            LogUpdate("_getElement " + by, Colors.Purple);
            try {
                return _driver.FindElement(by);
            }
            catch (NoSuchElementException) {
                return null;
            }
        }

        private ReadOnlyCollection<IWebElement> _getElements(By by) {
            LogUpdate("_getElements " + by, Colors.Purple);
            return _driver.FindElements(by);
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
