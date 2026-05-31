using System;
using System.Data.SqlClient;

public partial class _Default : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack && Session["Usuario"] != null)
            Response.Redirect("Menu.aspx", false);
    }

    protected void btnIngresar_Click(object sender, EventArgs e)
    {
        string usuario        = txtUsuario.Text.Trim().ToLower();
        string claveIngresada = Seguridad.HashSHA256(txtContrasena.Text);
        string ip             = Request.UserHostAddress;

        if (Application["DBDisponible"] != null && !(bool)Application["DBDisponible"])
        {
            lblError.Text    = "No esta disponible el sistema. Intente mas tarde.";
            lblError.Visible = true;
            return;
        }

        try
        {
            using (SqlConnection con = ConexionDB.ObtenerConexion())
            {
                con.Open();

                // Buscamos el usuario con esa contraseña hasheada (tabla no tiene columna Activo)
                SqlCommand cmd = new SqlCommand(
                    @"SELECT IdUsuario, NombreUsuario, Rol
                      FROM Usuarios
                      WHERE NombreUsuario=@u AND PasswordHash=@h", con);
                cmd.Parameters.AddWithValue("@u", usuario);
                cmd.Parameters.AddWithValue("@h", claveIngresada);

                SqlDataReader reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    Session["IdUsuario"] = (int)reader["IdUsuario"];
                    Session["Usuario"]   = reader["NombreUsuario"].ToString();
                    Session["Rol"]       = reader["Rol"].ToString();
                    reader.Close();
                    Bitacora.Registrar(usuario, "LOGIN", "Login exitoso - Rol: " + Session["Rol"]);
                    // false evita que Response.Redirect lance ThreadAbortException
                    Response.Redirect("Menu.aspx", false);
                    return;
                }
                else
                {
                    reader.Close();
                    Bitacora.Registrar(usuario, "LOGIN_FALLO", "Credenciales incorrectas");
                    lblError.Text    = "Usuario o contrasena incorrectos.";
                    lblError.Visible = true;
                }
            }
        }
        catch (Exception ex)
        {
            Bitacora.Registrar(usuario, "LOGIN_ERROR", ex.Message);
            lblError.Text    = "Error BD: " + ex.Message;
            lblError.Visible = true;
        }
    }
}
