using System;
using System.Data.SqlClient;

public partial class _Default : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack && Session["Usuario"] != null)
            Response.Redirect("Menu.aspx", false);
    }

    // ===== LOGIN =====

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

                // Columnas reales: IdUsuario, NombreUsuario, Rol (la tabla no tiene Activo)
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

    // Muestra el formulario de registro
    protected void btnIrRegistro_Click(object sender, EventArgs e)
    {
        lblError.Visible       = false;
        pnlLogin.Visible       = false;
        pnlRegistro.Visible    = true;
        lblRegMensaje.Visible  = false;
    }

    // Vuelve al formulario de login
    protected void btnIrLogin_Click(object sender, EventArgs e)
    {
        pnlRegistro.Visible   = false;
        pnlLogin.Visible      = true;
        lblRegMensaje.Visible = false;
    }

    // ===== REGISTRO =====

    protected void btnRegistrar_Click(object sender, EventArgs e)
    {
        string nombre = txtRegUsuario.Text.Trim().ToLower();
        string pass   = txtRegPass.Text;
        string conf   = txtRegConfirm.Text;

        // Validaciones basicas
        if (nombre == "" || pass == "")
        {
            MostrarRegError("El nombre de usuario y la contrasena son obligatorios.");
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

                // Verificamos que el nombre no este ya tomado
                SqlCommand check = new SqlCommand(
                    "SELECT COUNT(1) FROM Usuarios WHERE NombreUsuario=@n", con);
                check.Parameters.AddWithValue("@n", nombre);
                int existe = (int)check.ExecuteScalar();

                if (existe > 0)
                {
                    MostrarRegError("Ese nombre de usuario ya esta en uso. Elegi otro.");
                    return;
                }

                // Insertamos con rol Usuario por defecto
                string hash = Encriptacion.HashSHA256(pass);
                SqlCommand cmd = new SqlCommand(
                    "INSERT INTO Usuarios (NombreUsuario, PasswordHash, Rol) VALUES (@n, @h, 'Usuario')", con);
                cmd.Parameters.AddWithValue("@n", nombre);
                cmd.Parameters.AddWithValue("@h", hash);
                cmd.ExecuteNonQuery();
            }

            Bitacora.Registrar(nombre, "REGISTRO", "Nuevo usuario registrado desde el login");

            // Mostramos mensaje de exito y volvemos al login
            txtRegUsuario.Text = "";
            txtRegPass.Text    = "";
            txtRegConfirm.Text = "";
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
