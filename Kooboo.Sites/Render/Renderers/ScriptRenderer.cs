//Copyright (c) 2018 Yardi Technology Limited. Http://www.kooboo.com 
//All rights reserved.
using System.Threading.Tasks;
using Kooboo.Sites.Extensions;
using System.Text;

namespace Kooboo.Sites.Render
{
    public class ScriptRenderer
    {
        public static void Render(FrontContext context)
        {
            var script = context.SiteDb.Scripts.Get(context.Route.objectId);
            context.RenderContext.Response.ContentType = "application/javascript;charset=utf-8";
              
            string result = Getbody(context, script);

            TextBodyRender.SetBody(context, result);
        }

        private static string Getbody(FrontContext context, Models.Script script)
        {
             string result = null;
            if (script != null && script.Body != null)
            {
                if (script.Extension == null || script.Extension == "js" || script.Extension == ".js")
                {
                    result = script.Body;
                }
                else
                {
                    var engines = Kooboo.Sites.Engine.Manager.GetScript();

                    var find = engines.Find(o => o.Extension == script.Extension);
                    if (find != null)
                    {
                        result = find.Execute(context.RenderContext, script.Body);

                    }
                    else
                    {
                        result = script.Body;
                    }
                }
            }

            return result;
        }


    }
}
