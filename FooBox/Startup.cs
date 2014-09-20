using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(FooBox.Startup))]
namespace FooBox
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            // Nothing
        }
    }
}
