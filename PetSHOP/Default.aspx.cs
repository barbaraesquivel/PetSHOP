using System;
using System.Data.SqlClient;
using DAL;
using SEGURIDAD;
using SERV;

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
        string claveIngresada = Encriptacion.HashSHA256(txtContrasena.Text);

        if (Application["DBDisponible"] != null && !(bool)Application["DBDisponible"])
        {
            lblError.Text    = "No esta disponible el sistema. Intente mas tarde.";
            lblError.Visible = true;
            return;
        }

        try
        {
            using (SqlConnection con = ConexionBD.ObtenerConexion())
            {
                con.Open();

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
                    string rol           = reader["Rol"].ToString();
                    Session["Rol"]       = rol;
                    reader.Close();

                    bool bloqueado = Application["SistemaBlockeado"] != null && (bool)Application["SistemaBlockeado"];
                    if (bloqueado && rol != "WebMaster")
                    {
                        Session.Clear();
                        Session.Abandon();
                        Bitacora.Registrar(usuario, "LOGIN_BLOQUEADO", "Sistema bloqueado - acceso denegado");
                        Response.Redirect("Error.aspx?motivo=integridad", false);
                        return;
                    }

                    Bitacora.Registrar(usuario, "LOGIN", "Login exitoso - Rol: " + rol);
                    if (bloqueado)
                        Response.Redirect("WebMaster.aspx", false);
                    else
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


    protected void btnIrRegistro_Click(object sender, EventArgs e)
    {
        lblError.Visible       = false;
        pnlLogin.Visible       = false;
        pnlRegistro.Visible    = true;
        lblRegMensaje.Visible  = false;
    }

    protected void btnIrLogin_Click(object sender, EventArgs e)
    {
        pnlRegistro.Visible   = false;
        pnlLogin.Visible      = true;
        lblRegMensaje.Visible = false;
    }


    protected void btnRegistrar_Click(object sender, EventArgs e)
    {
        string nombre    = txtRegUsuario.Text.Trim().ToLower();
        string regNombre = txtRegNombre.Text.Trim();
        string apellido  = txtRegApellido.Text.Trim();
        string email     = txtRegEmail.Text.Trim();
        string telefono  = txtRegTelefono.Text.Trim();
        string direccion = txtRegDireccion.Text.Trim();
        string pass      = txtRegPass.Text;
        string conf      = txtRegConfirm.Text;

        if (nombre == "" || regNombre == "" || apellido == "" || email == "" || pass == "")
        {
            MostrarRegError("Usuario, nombre, apellido, email y contrasena son obligatorios.");
            return;
        }

        if (pass.Length < 4)
        {
            MostrarRegError("La contrasena debe tener al menos 4 caracteres.");
            return;
        }

        if (pass != conf)
        {
            MostrarRegError("Las contrasenas no coinciden.");
            return;
        }

        try
        {
            using (SqlConnection con = ConexionBD.ObtenerConexion())
            {
                con.Open();

                SqlCommand check = new SqlCommand(
                    "SELECT COUNT(1) FROM Usuarios WHERE NombreUsuario=@n", con);
                check.Parameters.AddWithValue("@n", nombre);
                int existe = (int)check.ExecuteScalar();

                if (existe > 0)
                {
                    MostrarRegError("Ese nombre de usuario ya esta en uso. Elegi otro.");
                    return;
                }

                string hash = Encriptacion.HashSHA256(pass);
                SqlCommand cmd = new SqlCommand(
                    @"INSERT INTO Usuarios (NombreUsuario, PasswordHash, Rol, Nombre, Apellido, Email, Telefono, Direccion)
                      VALUES (@n, @h, 'Usuario', @nombre, @apellido, @email, @telefono, @direccion)", con);
                cmd.Parameters.AddWithValue("@n",         nombre);
                cmd.Parameters.AddWithValue("@h",         hash);
                cmd.Parameters.AddWithValue("@nombre",    regNombre);
                cmd.Parameters.AddWithValue("@apellido",  apellido);
                cmd.Parameters.AddWithValue("@email",     email);
                cmd.Parameters.AddWithValue("@telefono",  telefono == "" ? (object)DBNull.Value : telefono);
                cmd.Parameters.AddWithValue("@direccion", direccion == "" ? (object)DBNull.Value : direccion);
                cmd.ExecuteNonQuery();
            }

            Bitacora.Registrar(nombre, "REGISTRO", "Nuevo usuario: " + regNombre + " " + apellido + " <" + email + ">");

            txtRegUsuario.Text  = "";
            txtRegNombre.Text   = "";
            txtRegApellido.Text = "";
            txtRegEmail.Text    = "";
            txtRegTelefono.Text = "";
            txtRegDireccion.Text = "";
            txtRegPass.Text     = "";
            txtRegConfirm.Text  = "";
            pnlRegistro.Visible = false;
            pnlLogin.Visible    = true;
            lblError.Text       = "Cuenta creada correctamente. Ya podes iniciar sesion.";
            lblError.CssClass   = "ok";
            lblError.Visible    = true;
        }
        catch (Exception ex)
        {
            MostrarRegError("Error al registrar: " + ex.Message);
        }
    }

    private void MostrarRegError(string texto)
    {
        lblRegMensaje.Text    = texto;
        lblRegMensaje.CssClass = "error";
        lblRegMensaje.Visible  = true;
    }
}
