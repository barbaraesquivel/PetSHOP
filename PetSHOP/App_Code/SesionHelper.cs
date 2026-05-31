using System.Web.UI;

public static class SesionHelper
{
    // Verifica que haya sesion activa; si no, redirige al login
    public static bool VerificarSesion(Page pagina)
    {
        if (pagina.Session["Usuario"] == null)
        {
            pagina.Response.Redirect("Default.aspx", false);
            return false;
        }
        return true;
    }

    // Verifica que el usuario tenga el nivel de rol requerido
    // Jerarquia: WebMaster > Admin > Usuario
    public static bool VerificarRol(Page pagina, string rolRequerido)
    {
        if (!VerificarSesion(pagina)) return false;

        string rolActual = pagina.Session["Rol"].ToString();

        if (rolRequerido == "Usuario")
            return true;

        if (rolRequerido == "Admin")
            return rolActual == "Admin" || rolActual == "WebMaster";

        if (rolRequerido == "WebMaster")
            return rolActual == "WebMaster";

        return false;
    }

    // Verifica que la BD este disponible segun el flag en Application
    // Devuelve false si Application["DBDisponible"] es false
    public static bool VerificarDB(Page pagina)
    {
        if (pagina.Application["DBDisponible"] != null && !(bool)pagina.Application["DBDisponible"])
            return false;
        return true;
    }
}
