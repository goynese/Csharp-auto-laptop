using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using HtmlAgilityPack;
using System.Diagnostics;
using SharpVoice;
using System.Net;
using System.Net.Mail;
using System.Text.RegularExpressions;
/*
    Had the classes in a seperate file. As well as the adding to cart, and checkout functions. I've moved all the code into this one file.
*/
namespace SeleniumLaptop
{
    class Program
    {
        public class Specs
        {
            public string processor { get; set; }

            public string display { get; set; }

            public string graphics { get; set; }

            public string memory { get; set; }

            public string hardDrive { get; set; }

            public string opticalDrive { get; set; }

            public string partNumber { get; set; }
        }

        public class Laptop : Specs
        {
            public string name { get; set; }

            public decimal price { get; set; }

            public string addtoCartLink { get; set; }
        }

        public static Func<IWebDriver, IWebElement> ElementIsClickable(By locator)
        {
            return driver =>
            {
                var element = driver.FindElement(locator);
                return (element != null && element.Displayed && element.Enabled) ? element : null;
            };
        }

        //Allows for cashback when automatically purchasing laptops.
        static void FatWalletLogin(ChromeDriver driver)
        {
            //Allows waiting for elements to become visable.
            WebDriverWait wait = new WebDriverWait(driver, new TimeSpan(0, 0, 40));


            //Navigates to Lenovo Homepage with the 3% cash back enabled. 
            driver.Navigate().GoToUrl("http://www.fatwallet.com/interstitial/signin?targetUrl=http%3A%2F%2Fwww.fatwallet.com%2Fticket%2Fstore%2FLenovo%3Fs%3Dstorepage");

            //Login then it navigates to Lenovo Homepage
            wait.Until(ExpectedConditions.ElementIsVisible(By.LinkText("Sign In"))).Click();
            wait.Until(ExpectedConditions.ElementIsVisible(By.Id("loginEmailAddress"))).SendKeys("fakeemail@yahoo.com");
            wait.Until(ExpectedConditions.ElementIsVisible(By.Id("loginPassword"))).SendKeys("fakepassword");
            wait.Until(ExpectedConditions.ElementIsVisible(By.CssSelector("div.authenticationFormButton > div > #signupsubmitbtn"))).Click();

            //Wait for lenovo.com to load
            Thread.Sleep(3500);
        }
        
        static int CheckOut(ChromeDriver driver, Laptop laptop)
        {
            using (driver)
            {
                WebDriverWait wait = new WebDriverWait(driver, new TimeSpan(0, 0, 40));

            BeginCheckout:

                //Navigate to Cart
                driver.Navigate().GoToUrl("http://shop.lenovo.com/SEUILibrary/controller/e/outlet_us/LenovoPortal/en_US/cart.workflow:ShowCart");

                if (driver.PageSource.Contains("currently"))
                {
                    return 0;
                }

                wait.Until(ExpectedConditions.ElementIsVisible(By.Id("cart-item-[:00000128:]"))).Clear();
                wait.Until(ExpectedConditions.ElementIsVisible(By.Id("cart-item-[:00000128:]"))).SendKeys("6");
                wait.Until(ExpectedConditions.ElementIsVisible(By.Id("cart-item-pricing-and-quantity-form-button"))).Click();

                Thread.Sleep(3000);

                //Proceed to checkout
                //driver.FindElement(By.LinkText("Proceed to Checkout")).Click();

                try
                {
                    driver.FindElement(By.ClassName("cart-checkoutButtons-checkout")).Click();
                }
                catch
                {
                    goto BeginCheckout;
                }
               //wait.Until(ExpectedConditions.ElementIsVisible(By.LinkText("cart-checkoutButtons-checkout"))).Click();


                //Sign in if not signed in.
                wait.Until(ElementIsClickable(By.Id("LoginName")));
                driver.FindElement(By.Id("LoginName")).Clear();
                driver.FindElement(By.Id("LoginName")).SendKeys("eddiegoynes@yahoo.com");
                driver.FindElement(By.Id("Password")).Clear();
                driver.FindElement(By.Id("Password")).SendKeys("password");

                ////wait.Until(ElementIsClickable(By.CssSelector("#SignInButtonLink > span"))).Click();
                try
                {
                    Thread.Sleep(1000);
                    wait.Until(ExpectedConditions.ElementIsVisible(By.CssSelector("#SignInButtonLink > span"))).Click();
                }
                catch
                {
                    Thread.Sleep(5000);
                    wait.Until(ExpectedConditions.ElementIsVisible(By.Id("SignInButtonLink"))).Click();
                }

                //driver.FindElement(By.CssSelector("#SignInButtonLink > span")).Click();

                if (driver.PageSource.Contains("Opinion!"))
                {
                    wait.Until(ElementIsClickable(By.Id("oo_never_show"))).Click();
                }


                //Enter Address
                try
                {
                    Thread.Sleep(3000);
                    wait.Until(ElementIsClickable(By.CssSelector("#continueButton > span"))).Click();
                }
                catch
                {
                    driver.Navigate().GoToUrl("http://shop.lenovo.com/SEUILibrary/controller/e/outlet_us/LenovoPortal/en_US/cart.workflow:ShowCart");
                    driver.FindElement(By.ClassName("cart-checkoutButtons-checkout")).Click();
                    Thread.Sleep(3000);
                    wait.Until(ElementIsClickable(By.CssSelector("#continueButton > span"))).Click();
                }

                //Enter Credit Card Information
                //wait.Until(ExpectedConditions.ElementExists(By.XPath("//*[@id=\"table\"]/div[1]/div[2]")));
                try
                {
                    wait.Until(ExpectedConditions.ElementExists(By.ClassName("paymentTokenizationFrame")));
                }
                catch
                {
                    SendEmailMessage("Error payment page broken", "FF");
                    goto BeginCheckout;
                }
                driver.SwitchTo().Frame(driver.FindElementByClassName("paymentTokenizationFrame"));



                Thread.Sleep(3000);
                new SelectElement(driver.FindElement(By.Id("Paymetric_CreditCardType"))).SelectByText("Visa");

                IWebElement editable = driver.SwitchTo().ActiveElement();
                editable.SendKeys("Your text here");

                driver.FindElement(By.Id("Paymetric_CreditCardNumber")).SendKeys("ChaseFreedomCard#");
                driver.FindElement(By.Id("Paymetric_Exp_Month")).SendKeys("12");
                driver.FindElement(By.Id("Paymetric_Exp_Year")).SendKeys("12");
                driver.FindElement(By.Id("Paymetric_CVV")).SendKeys("123");

                driver.SwitchTo().DefaultContent();

                driver.FindElement(By.Id("CARD_HOLDER_NAME")).SendKeys("John Doe");

                try
                {
                    Thread.Sleep(2000);
                    wait.Until(ElementIsClickable(By.Id("checkout-continue-billing-link"))).Click();
                }
                catch
                {
                    driver.Navigate().GoToUrl("http://shop.lenovo.com/SEUILibrary/controller/e/outlet_us/LenovoPortal/en_US/cart.workflow:ShowCart");
                    driver.FindElement(By.ClassName("cart-checkoutButtons-checkout")).Click();
                    Thread.Sleep(1000);
                    wait.Until(ElementIsClickable(By.CssSelector("#continueButton > span"))).Click();

                    //CREDIT CARDS
                    new SelectElement(driver.FindElement(By.Id("Paymetric_CreditCardType"))).SelectByText("Visa");

                    editable = driver.SwitchTo().ActiveElement();
                    editable.SendKeys("Your text here");

                    driver.FindElement(By.Id("Paymetric_CreditCardNumber")).SendKeys("CardNumber");
                    driver.FindElement(By.Id("Paymetric_Exp_Month")).SendKeys("12");
                    driver.FindElement(By.Id("Paymetric_Exp_Year")).SendKeys("12");
                    driver.FindElement(By.Id("Paymetric_CVV")).SendKeys("123");

                    driver.SwitchTo().DefaultContent();

                    driver.FindElement(By.Id("CARD_HOLDER_NAME")).SendKeys("Edward Goynes");

                    //
                    Thread.Sleep(2000);
                    wait.Until(ElementIsClickable(By.Id("checkout-continue-billing-link"))).Click();
                }

                //Place Order
                wait.Until(ElementIsClickable(By.Id("TERMS_AND_CONDITIONS_AGREEMENT"))).Click();
                driver.FindElement(By.Id("checkout-continue-review-btn-link")).Click();
   
                //Email Successful purchase
                SendEmailMessage(laptop.name + laptop.processor + "Total Price:" + laptop.price, "Checkout Successful Price: ");

                //Laptop added to cart 
                return 1;
            }
        }

        static void SendEmailMessage(string message, string subject , long telephoneNumer = 5418293604)
        {
            // login details for gmail acct.
            const string sender = "fakeemail@gmail.com";
            const string password = "emailpassword";

            // find the carriers sms gateway for the recipent. txt.att.net is for AT&T customers.
            //string carrierGateway = "messaging.sprintpcs.com";

            // this is the recipents number @ carrierGateway that gmail use to deliver message.
            string recipent = "eddiewgoynes@gmail.com";

            // form the text message and send
            using (MailMessage textMessage = new MailMessage(sender, recipent, subject + "FatCAT", message))
            {
                using (SmtpClient textMessageClient = new SmtpClient("smtp.gmail.com", 587))
                {
                    textMessageClient.UseDefaultCredentials = false;
                    textMessageClient.EnableSsl = true;
                    textMessageClient.Credentials = new NetworkCredential(sender, password);
                    textMessageClient.Send(textMessage);
                }
            }
        }

        private static List<Laptop> GetLaptopListFromURL(ChromeDriver driver, string laptopListUrl)
        {
            var timesRefreshed = 0;
            var watch = Stopwatch.StartNew();
            start:
                //Timer
                IEnumerable<HtmlNode> laptopAddCartLinkNode = null;
                List<Laptop> laptopList = new List<Laptop>();
                HtmlDocument laptopHtml = new HtmlDocument();
                HtmlDocument listPage = new HtmlDocument();
                char[] MyChar = { '\r', '\n', '\t', '$', '\\' };

                watch.Reset();
                //Refresh the page ignoring Chrome's cache.
                //driver.SendKeys(Keys.Shift + Keys.F5);
                //driver.Navigate().Refresh();
                driver.Navigate().GoToUrl(laptopListUrl);
                if(timesRefreshed > 20)
                {
                    return null;
                }
                timesRefreshed++;
                watch.Restart();

                for (int i = 0; i < 30; i++)
                {
                    //Load the html into a page. And Do a Try catch since this can fail 1 out of 100 times.
                    try
                    {
                        listPage.Load(new StringReader(driver.ExecuteScript("return document.getElementsByTagName('html')[0].innerHTML").ToString()));
                    }
                    catch
                    {
                        goto start;
                    }

                    Thread.Sleep(80);

                    //Check if the page has the laptop in it.
                    laptopAddCartLinkNode = listPage.DocumentNode.Descendants().Where(x => x.Attributes.Contains("class") && (x.Attributes["class"].Value == "aftercoupon pricingSummary-details-final-price"));
                    if (laptopAddCartLinkNode != null && laptopAddCartLinkNode.Count() > 0)
                    {
                        try
                        {
                            driver.ExecuteScript("window.scrollTo(0,document.body.scrollHeight);");
                            Thread.Sleep(40);
                            driver.ExecuteScript("window.scrollTo(0,document.body.scrollHeight);");
                            Thread.Sleep(40);
                            driver.ExecuteScript("window.scrollTo(0,document.body.scrollHeight);");
                            Thread.Sleep(70);
                            driver.ExecuteScript("window.scrollTo(0,document.body.scrollHeight);");
                            Thread.Sleep(70);


                            listPage.Load(new StringReader(driver.ExecuteScript("return document.getElementsByTagName('html')[0].innerHTML").ToString()));
                        }
                        catch
                        {
                            goto start;
                        }
                        laptopAddCartLinkNode = listPage.DocumentNode.Descendants().Where(x => x.Attributes.Contains("class") && (x.Attributes["class"].Value == "aftercoupon pricingSummary-details-final-price"));
                        break;
                    }
                }


                //If there are no laptops currently listed.
                if (laptopAddCartLinkNode == null || laptopAddCartLinkNode.Count() == 0)
                {
                    Thread.Sleep(3000);
                    goto start;
                }
                   

                //Time the project.
                watch.Stop();
                Console.WriteLine(watch.ElapsedMilliseconds.ToString() + "ms");

                                    //(int.Parse(x.InnerText.TrimStart('$')) < maxPrice)).ParentNode.ParentNode.ParentNode.Descendants().FirstOrDefault(x => x.HasAttributes && x.Attributes["Class"].Value == "button shop fluid");

                if (laptopAddCartLinkNode != null && laptopAddCartLinkNode.Count() > 0 && laptopAddCartLinkNode.Any(x => x.InnerText.Trim(MyChar).Length != 0))
                {
                    //Change to list of laptops below 535

                    laptopAddCartLinkNode = laptopAddCartLinkNode.Where(x => decimal.Parse(x.InnerText.Trim(MyChar)) < 680).OrderBy(x => decimal.Parse(x.InnerText.Trim(MyChar)));
                    if (laptopAddCartLinkNode.Count() > 0)
                    {
                        foreach (var laptopCartLink in laptopAddCartLinkNode)
                        {
                            var price = decimal.Parse(laptopCartLink.InnerText.Trim(MyChar));

                            //Get add to cart link
                            laptopHtml.Load(new StringReader(laptopCartLink.ParentNode.ParentNode.ParentNode.ParentNode.InnerHtml));

                            var addToCartLink = laptopHtml.DocumentNode.SelectNodes("//input[@class='pn']").FirstOrDefault().Attributes.FirstOrDefault(x => x.Name == "value").Value;

                            //Title
                            laptopHtml.Load(new StringReader(laptopCartLink.ParentNode.ParentNode.ParentNode.ParentNode.ParentNode.ParentNode.ParentNode.InnerHtml));

                            //Title of the laptop

                            var title = laptopHtml.DocumentNode.SelectNodes("//div[@class='facetedResults-header']").Descendants("a").Where(x => x.Attributes.Contains("title") && x.Attributes["title"].Value.Contains("View details")).FirstOrDefault().InnerHtml;

                            var specs = laptopHtml.DocumentNode.SelectNodes("//div[@class='facetedResults-feature-list']").FirstOrDefault().InnerText;

                            //var title = laptopHtml.DocumentNode.SelectNodes("/h3/a").FirstOrDefault().InnerText.ToString();

                            //Specs of laptop

                            //laptopCartLink.ParentNode.ParentNode.ParentNode.ParentNode.ParentNode.ParentNode.Descendants().Where(x => x.Attributes.Contains("class") && x.Attributes["class"].Value == "facetedResults-feature-list").FirstOrDefault().InnerHtml.ToString().Replace("\n", "").ToString();
                            //String LaptopSpecs = Regex.Replace(laptopSpecs, @"<[^>]*>", "");

                            laptopList.Add(new Laptop()
                            {
                                addtoCartLink = "http://shop.lenovo.com/SEUILibrary/controller/e/outlet_us/LenovoPortal/en_US/config.workflow:ConfigureMtmAsItem?mtm-item=" + addToCartLink.ToString() + "&amp;action=addtocart",
                                name = title,
                                price = price,
                                processor = specs
                            });
                        }
                    }
                    else
                    {
                        Thread.Sleep(3000);

                        goto start;
                    }

                    //List of Laptop objects, includes all of the laptops on the page. 
                    return laptopList;
                }
                Thread.Sleep(3000);
                goto start;
        }

        private static int AddToCart(ChromeDriver driver, List<Laptop> laptopList)
        {
            WebDriverWait wait = new WebDriverWait(driver, new TimeSpan(0, 0, 20));
            
            foreach(var laptop in laptopList)
            {
                driver.Navigate().GoToUrl(laptop.addtoCartLink);

                Thread.Sleep(3000);

                //This takes care of a corner case when the webpage wants you to cutomize your order.
                if (!driver.PageSource.Contains("the page you're looking for can't be found.") || !driver.PageSource.Contains("CURRENTLY EMPTY."))
                {
                    if (driver.PageSource.Contains("Enhance your"))
                    {
                        try
                        {
                            wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("//*[@id=\"builderv2Tab9999999999999999\"]/a"))).Click();
                        }
                        catch
                        {
                            return -1;
                        }

                        wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("//*[@id=\"builderv2AddToCart\"]"))).Click();
                    }
                    else if (driver.PageSource.Contains("YOUR SYSTEM SUMMARY"))
                    {
                        //These conditions only happen approximently 10% of the time. 
                        wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("//*[@id=\"builderv2Tab9999999999999999\"]/a"))).Click();

                        wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("//*[@id=\"builderv2AddToCart\"]"))).Click();
                    }
                    Thread.Sleep(1000);
                    if (driver.PageSource.Contains("business days"))
                    {
                        //Success the cheap Ass laptop has been added to the cart.
                        Console.WriteLine("Success the cheap Ass laptop has been added to the cart.");
                        
                        SendEmailMessage( "This Laptop Cost = " + laptop.price.ToString(), "Laptop Cart, Price = " + laptop.price.ToString());

                        //Automatically Check out for me. :()
                        return CheckOut(driver, laptop);
                    }
                    else
                    {
                        //Falure to add the laptop to the cart.
                        Console.Write("Failure to add the laptop to the cart"); 
                        return -1;
                    }
                }
            }
            //Program restarts. 
            return 0;
        }

        static void Main(string[] args)
        {
        restart:
            // Initialize the Chrome Driver
            using (var driver = new ChromeDriver())
            {
                var addedToCart = 0;
                var timesRefeshed = 0;
                // Go to the home page
                FatWalletLogin(driver);

                var laptopListUrl = "http://outlet.lenovo.com/SEUILibrary/controller/e/outlet_us/LenovoPortal/en_US/catalog.workflow:show-category?category-id=908B184AED4F29502E6EB3E1E76AFC13#/?page-index=1&facet-2=1&facet-6=1&facet-8=12";

                //Continously look at the page for new laptop listings.
                while (true)
                {
                    //Get list of all laptops on page.
                    List<Laptop> laptopList = GetLaptopListFromURL(driver, laptopListUrl);

                    if (laptopList == null)
                        goto restart;

                    //Add only certain laptops to cart and buy
                    addedToCart = AddToCart(driver, laptopList
                        .Where(x => x.price <= 680 && x.name.Contains("Y50") && !x.name.Contains("Y500") ||
                            x.price < 600 && x.name.Contains("Thinkpad S1 Yoga") ||
                            x.price < 620 && x.name.Contains("T440") ||
                            x.price < 680 && x.name.Contains("W540") ||
                            x.price < 680 && x.name.Contains("X1") ||
                            x.price < 580 && x.name.Contains("Thinkpad")
                        ).ToList());

                    timesRefeshed++;

                    if (addedToCart == 1 || timesRefeshed > 20)
                        goto restart;

                    //Need to wait in ensure ip not being banned.
                    Thread.Sleep(3000);
                };
            }
        }
    }
}
