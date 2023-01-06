using AndHowIsItApp.Resources;
using System.Globalization;
using System.Threading;
using System.Web;
using System.Web.Mvc;

namespace AndHowIsItApp.Controllers
{
    public class InterfaceController : Controller
    {
        public ActionResult SetLanguage(string language)
        {
            var returnUrl = Request.UrlReferrer;
            if (!string.IsNullOrEmpty(language))
            {
                Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture(language);
                Thread.CurrentThread.CurrentUICulture = new CultureInfo(language);
            }
            HttpCookie cookie = new HttpCookie("Languages");
            cookie.Value = language;
            Response.Cookies.Add(cookie);
            if (returnUrl != null && Url.IsLocalUrl(returnUrl.PathAndQuery))
            {
                return Redirect(returnUrl.PathAndQuery);
            }
            return RedirectToAction("Index", "Home");
        }

        public ActionResult SetTheme(string theme)
        {
            var returnUrl = Request.UrlReferrer;
            if (!string.IsNullOrEmpty(theme))
            {
                HttpCookie cookie = new HttpCookie("Theme");
                cookie.Value = theme;
                Response.Cookies.Add(cookie);
            }
            if (returnUrl != null && Url.IsLocalUrl(returnUrl.PathAndQuery))
            {
                return Redirect(returnUrl.PathAndQuery);
            }
            return RedirectToAction("Index", "Home");
        }
    }
}