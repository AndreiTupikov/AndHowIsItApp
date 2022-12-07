using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(AndHowIsItApp.Startup))]
namespace AndHowIsItApp
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
