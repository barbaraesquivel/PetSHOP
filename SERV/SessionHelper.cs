using System.Web.UI;

namespace SERV
{
    public static class SessionHelper
    {
        public static bool VerificarSesion(Page pagina)
        {
            if (pagina.Session["Usuario"] == null)
            {
                pagina.Response.Redirect("Default.aspx", false);
                return false;
            }
            return true;
        }

        public static bool VerificarRol(Page pagina, string rolRequerido)
        {
            if (!VerificarSesion(pagina)) return false;

            string rolActual = pagina.Session["Rol"].ToString();

            if (rolRequerido == "Usuario")  return true;
            if (rolRequerido == "Admin")    return rolActual == "Admin" || rolActual == "WebMaster";
            if (rolRequerido == "WebMaster") return rolActual == "WebMaster";

            return false;
        }

        public static bool VerificarDB(Page pagina)
        {
            if (pagina.Application["DBDisponible"] != null && !(bool)pagina.Application["DBDisponible"])
                return false;
            return true;
        }
    }
}
