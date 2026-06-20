using System;
using System.Data.SqlClient;
using DAL;
using SERV;

public partial class MiPerfil : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        if (!SessionHelper.VerificarRol(this, "Usuario")) return;

        if (!SessionHelper.VerificarDB(this)) return;

        lblNombreUsuario.Text = Session["Usuario"].ToString();

        // Mostrar aviso si los datos obligatorios estan vacios
        if (!IsPostBack)
        {
            CargarDatos();
        }
    }

    private void CargarDatos()
    {
        int idUsuario = (int)Session["IdUsuario"];

        using (SqlConnection con = ConexionBD.ObtenerConexion())
        {
            con.Open();
            SqlCommand cmd = new SqlCommand(
                "SELECT Nombre, Apellido, Email, Telefono, Direccion FROM Usuarios WHERE IdUsuario=@id", con);
            cmd.Parameters.AddWithValue("@id", idUsuario);
            SqlDataReader r = cmd.ExecuteReader();
            if (r.Read())
            {
                txtNombre.Text    = r["Nombre"]    == DBNull.Value ? "" : r["Nombre"].ToString();
                txtApellido.Text  = r["Apellido"]  == DBNull.Value ? "" : r["Apellido"].ToString();
                txtEmail.Text     = r["Email"]     == DBNull.Value ? "" : r["Email"].ToString();
                txtTelefono.Text  = r["Telefono"]  == DBNull.Value ? "" : r["Telefono"].ToString();
                txtDireccion.Text = r["Direccion"] == DBNull.Value ? "" : r["Direccion"].ToString();

                // Mostrar aviso si le faltan datos obligatorios
                bool faltanDatos = string.IsNullOrEmpty(txtNombre.Text)
                                || string.IsNullOrEmpty(txtApellido.Text)
                                || string.IsNullOrEmpty(txtEmail.Text);
                pnlAviso.Visible = faltanDatos;
            }
            r.Close();
        }
    }

    protected void btnGuardar_Click(object sender, EventArgs e)
    {
        string nombre    = txtNombre.Text.Trim();
        string apellido  = txtApellido.Text.Trim();
        string email     = txtEmail.Text.Trim();
        string telefono  = txtTelefono.Text.Trim();
        string direccion = txtDireccion.Text.Trim();

        if (nombre == "" || apellido == "" || email == "")
        {
            MostrarMensaje("Nombre, apellido y email son obligatorios.", esError: true);
            return;
        }

        int idUsuario = (int)Session["IdUsuario"];

        try
        {
            using (SqlConnection con = ConexionBD.ObtenerConexion())
            {
                con.Open();
                SqlCommand cmd = new SqlCommand(
                    @"UPDATE Usuarios
                      SET Nombre=@nombre, Apellido=@apellido, Email=@email,
                          Telefono=@telefono, Direccion=@direccion
                      WHERE IdUsuario=@id", con);
                cmd.Parameters.AddWithValue("@nombre",    nombre);
                cmd.Parameters.AddWithValue("@apellido",  apellido);
                cmd.Parameters.AddWithValue("@email",     email);
                cmd.Parameters.AddWithValue("@telefono",  telefono == "" ? (object)DBNull.Value : telefono);
                cmd.Parameters.AddWithValue("@direccion", direccion == "" ? (object)DBNull.Value : direccion);
                cmd.Parameters.AddWithValue("@id",        idUsuario);
                cmd.ExecuteNonQuery();
            }

            Bitacora.Registrar(Session["Usuario"].ToString(), "PERFIL_ACTUALIZADO",
                nombre + " " + apellido + " <" + email + ">");

            pnlAviso.Visible = false;
            MostrarMensaje("Datos guardados correctamente.", esError: false);
        }
        catch (Exception ex)
        {
            MostrarMensaje("Error al guardar: " + ex.Message, esError: true);
        }
    }

    protected void btnVolver_Click(object sender, EventArgs e)
    {
        Response.Redirect("Menu.aspx");
    }

    private void MostrarMensaje(string texto, bool esError)
    {
        lblMensaje.Text     = texto;
        lblMensaje.CssClass = "msg " + (esError ? "msg-err" : "msg-ok");
        lblMensaje.Visible  = true;
    }
}
