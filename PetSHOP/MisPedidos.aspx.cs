using System;
using System.Data;
using System.Web.UI.WebControls;
using BE;
using BLL;
using DAL;
using SERV;

public partial class MisPedidos : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        if (!SessionHelper.VerificarRol(this, "Usuario")) return;

        bool bloqueado = Application["SistemaBlockeado"] != null && (bool)Application["SistemaBlockeado"];
        if (bloqueado)
        {
            string rol = Session["Rol"] != null ? Session["Rol"].ToString() : "";
            Response.Redirect(rol == "WebMaster" ? "WebMaster.aspx" : "Error.aspx", false);
            return;
        }

        if (!SessionHelper.VerificarDB(this)) return;

        lblUsuario.Text = Session["Usuario"].ToString();

        if (!IsPostBack)
            CargarPedidos();
    }

    private void CargarPedidos()
    {
        int idUsuario = (int)Session["IdUsuario"];
        Cliente cliente = ClienteBLL.GetByIdUsuario(idUsuario);

        if (cliente == null)
        {
            lblSinPedidos.Visible    = true;
            gvMisPedidos.Visible     = false;
            pnlAlertaRetiro.Visible  = false;
            return;
        }

        try
        {
            DataTable dt = PedidoBLL.GetByCliente(cliente.IdCliente);

            if (dt.Rows.Count == 0)
            {
                lblSinPedidos.Visible   = true;
                gvMisPedidos.Visible    = false;
                pnlAlertaRetiro.Visible = false;
                return;
            }

            lblSinPedidos.Visible = false;
            gvMisPedidos.Visible  = true;

            // Alerta si algun pedido esta listo para retirar
            string alertaTexto = "";
            foreach (DataRow row in dt.Rows)
            {
                if (row["Estado"].ToString() == "ListoParaRetirar")
                {
                    if (alertaTexto != "") alertaTexto += ", ";
                    alertaTexto += "#" + row["IdPedido"];
                }
            }

            if (alertaTexto != "")
            {
                pnlAlertaRetiro.Visible = true;
                lblAlertaRetiro.Text    = "Tu pedido esta listo para retirar: " + alertaTexto;
            }
            else
            {
                pnlAlertaRetiro.Visible = false;
            }

            gvMisPedidos.DataSource = dt;
            gvMisPedidos.DataBind();
        }
        catch (Exception ex)
        {
            MostrarMensaje("Error al cargar pedidos: " + ex.Message, true);
        }
    }

    protected void gvMisPedidos_RowDataBound(object sender, GridViewRowEventArgs e)
    {
        if (e.Row.RowType != DataControlRowType.DataRow) return;

        DataRowView drv    = (DataRowView)e.Row.DataItem;
        string      estado = drv["Estado"].ToString();

        Button btnCancelar = (Button)e.Row.FindControl("btnCancelarPedido");
        if (btnCancelar != null)
            btnCancelar.Visible = PedidoBLL.PuedeCancelarCliente(estado);
    }

    protected void gvMisPedidos_RowCommand(object sender, GridViewCommandEventArgs e)
    {
        int idPedido = int.Parse(e.CommandArgument.ToString());

        if (e.CommandName == "VerDetalle")
        {
            try
            {
                DataTable dt = PedidoBLL.GetDetalleByPedido(idPedido);
                lblDetalleTitulo.Text   = "Detalle del pedido #" + idPedido;
                gvDetalle.DataSource    = dt;
                gvDetalle.DataBind();
                pnlDetalle.Visible      = true;
            }
            catch (Exception ex)
            {
                MostrarMensaje("Error al cargar detalle: " + ex.Message, true);
            }
        }
        else if (e.CommandName == "CancelarPedido")
        {
            try
            {
                string usr          = Session["Usuario"].ToString();
                string estadoPrevio = PedidoBLL.Cancelar(idPedido, usr, esAdmin: false);
                Bitacora.Registrar(usr, "PEDIDO_CANCELADO", "Pedido #" + idPedido + ": " + estadoPrevio + " -> Cancelado (cliente)");
                MostrarMensaje("Pedido #" + idPedido + " cancelado. El stock fue restaurado.", false);
                pnlDetalle.Visible = false;
                CargarPedidos();
            }
            catch (Exception ex)
            {
                MostrarMensaje("No se pudo cancelar: " + ex.Message, true);
            }
        }
    }

    protected void btnCerrarDetalle_Click(object sender, EventArgs e)
    {
        pnlDetalle.Visible = false;
    }

    protected void btnCerrarSesion_Click(object sender, EventArgs e)
    {
        Bitacora.Registrar(Session["Usuario"].ToString(), "LOGOUT", "Cerro sesion");
        Session.Clear();
        Session.Abandon();
        Response.Redirect("Default.aspx");
    }

    private void MostrarMensaje(string texto, bool esError)
    {
        lblMensaje.Text     = texto;
        lblMensaje.CssClass = "msg " + (esError ? "msg-err" : "msg-ok");
        lblMensaje.Visible  = true;
    }
}
