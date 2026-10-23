import { useCallback, useEffect, useState } from "react";
import {
  AlertCircle,
  ArrowDownToLine,
  BellRing,
  CheckCircle2,
  ChevronDown,
  CircleDollarSign,
  FileText,
  LoaderCircle,
  RefreshCw,
  Search,
  ShieldAlert,
  UsersRound,
} from "lucide-react";
import { EstadoBadge } from "./components/EstadoBadge";
import { ResumenCard } from "./components/ResumenCard";
import { facturasApi, type Factura, type ResumenFacturas } from "./services/facturas";

type FiltroEstado = "todos" | "primerrecordatorio" | "segundorecordatorio" | "desactivado" | "otros";

const resumenInicial: ResumenFacturas = {
  total: 0,
  primerRecordatorio: 0,
  segundoRecordatorio: 0,
  desactivadas: 0,
  otrosEstados: 0,
  facturas: [],
};

const formatoMoneda = new Intl.NumberFormat("es-CO", {
  style: "currency",
  currency: "COP",
});

function App() {
  const [resumen, setResumen] = useState(resumenInicial);
  const [busqueda, setBusqueda] = useState("");
  const [filtro, setFiltro] = useState<FiltroEstado>("todos");
  const [cargando, setCargando] = useState(true);
  const [procesando, setProcesando] = useState(false);
  const [apiConectada, setApiConectada] = useState(false);
  const [error, setError] = useState("");
  const [aviso, setAviso] = useState("");

  const cargarResumen = useCallback(async () => {
    setCargando(true);
    setError("");
    try {
      setResumen(await facturasApi.obtenerResumen());
      setApiConectada(true);
    } catch (exception) {
      setApiConectada(false);
      setError(exception instanceof Error ? exception.message : "No se pudo cargar el resumen.");
    } finally {
      setCargando(false);
    }
  }, []);

  useEffect(() => {
    void cargarResumen();
  }, [cargarResumen]);

  const procesarRecordatorios = async () => {
    setProcesando(true);
    setAviso("");
    setError("");
    try {
      const resultado = await facturasApi.procesarRecordatorios();
      setAviso(
        `Proceso finalizado: ${resultado.procesadas} correo(s) enviados y ${resultado.fallidas} con error.`,
      );
      await cargarResumen();
      if (resultado.errores.length > 0) setError(resultado.errores.join(" · "));
    } catch (exception) {
      setApiConectada(false);
      setError(exception instanceof Error ? exception.message : "No se pudo procesar los recordatorios.");
    } finally {
      setProcesando(false);
    }
  };

  const facturasFiltradas = resumen.facturas.filter((factura) => {
    const coincideBusqueda = `${factura.nombreCliente} ${factura.correoCliente}`
      .toLowerCase()
      .includes(busqueda.toLowerCase());
    const coincideEstado =
      filtro === "todos" ||
      (filtro === "otros"
        ? !["primerrecordatorio", "segundorecordatorio", "desactivado"].includes(factura.estado.toLowerCase())
        : factura.estado.toLowerCase() === filtro);
    return coincideBusqueda && coincideEstado;
  });

  return (
    <main className="page-shell">
      <aside className="sidebar">
        <a className="brand" href="/" aria-label="Facturix inicio">
          <span className="brand-mark"><CircleDollarSign size={23} /></span>
          <span>facturix<span className="brand-dot">.</span></span>
        </a>
        <p className="nav-caption">ESPACIO DE TRABAJO</p>
        <a className="nav-item active" href="#facturas"><FileText size={18} /> Facturas</a>
        <div className="sidebar-bottom">
          <div className="profile">
            <div className="avatar">AD</div>
            <div><strong>Administración</strong><span>Panel de control</span></div>
            <ChevronDown size={16} className="profile-chevron" />
          </div>
        </div>
      </aside>

      <section className="main-area">
        <header className="topbar">
          <div className="breadcrumb">Facturación <span>/</span> <strong>Resumen</strong></div>
          <div className="topbar-actions">
            <span className={`connection ${apiConectada ? "" : "offline"}`}>
              <span /> {apiConectada ? "Sistema conectado" : "Sin conexión"}
            </span>
            <button className="icon-button" aria-label="Notificaciones"><BellRing size={18} /></button>
            <div className="avatar small">AD</div>
          </div>
        </header>

        <div className="content">
          <section className="page-heading">
            <div>
              <p className="eyebrow">GESTIÓN DE FACTURAS</p>
              <h1>Facturas</h1>
              <p className="subtitle">Supervisa tus facturas y el estado de los recordatorios.</p>
            </div>
            <div className="heading-actions">
              <button className="button button-secondary" onClick={() => void cargarResumen()} disabled={cargando}>
                <RefreshCw size={16} className={cargando ? "spin" : ""} /> Actualizar
              </button>
              <button className="button button-primary" onClick={() => void procesarRecordatorios()} disabled={procesando}>
                {procesando ? <LoaderCircle size={16} className="spin" /> : <ArrowDownToLine size={16} />}
                {procesando ? "Procesando..." : "Procesar recordatorios"}
              </button>
            </div>
          </section>

          {error && <div className="notice notice-error"><AlertCircle size={18} />{error}</div>}
          {aviso && <div className="notice notice-success"><CheckCircle2 size={18} />{aviso}</div>}

          <section className="summary-grid" id="resumen" aria-label="Resumen de facturas">
            <ResumenCard titulo="Total de facturas" valor={resumen.total} detalle="En todos los estados" icono={FileText} tono="blue" />
            <ResumenCard titulo="Primer recordatorio" valor={resumen.primerRecordatorio} detalle="Pendientes de seguimiento" icono={BellRing} tono="amber" />
            <ResumenCard titulo="Segundo recordatorio" valor={resumen.segundoRecordatorio} detalle="Aviso final enviado" icono={AlertCircle} tono="orange" />
            <ResumenCard titulo="Desactivadas" valor={resumen.desactivadas} detalle="Servicio desactivado" icono={ShieldAlert} tono="red" />
          </section>

          <section className="table-panel" id="facturas">
            <div className="table-heading">
              <div>
                <h2>Todas las facturas</h2>
                <p>Consulta el detalle y el estado de cada cliente.</p>
              </div>
              <span className="record-count">{facturasFiltradas.length} registros</span>
            </div>
            <div className="table-controls">
              <label className="search-box">
                <Search size={17} />
                <input
                  aria-label="Buscar cliente"
                  placeholder="Buscar cliente..."
                  value={busqueda}
                  onChange={(event) => setBusqueda(event.target.value)}
                />
              </label>
              <label className="filter-select">
                <span className="visually-hidden">Filtrar por estado</span>
                <select value={filtro} onChange={(event) => setFiltro(event.target.value as FiltroEstado)}>
                  <option value="todos">Todos los estados</option>
                  <option value="primerrecordatorio">Primer recordatorio</option>
                  <option value="segundorecordatorio">Segundo recordatorio</option>
                  <option value="desactivado">Desactivado</option>
                  <option value="otros">Otros</option>
                </select>
                <ChevronDown size={16} />
              </label>
            </div>
            <div className="table-scroll">
              <table>
                <thead>
                  <tr><th>CLIENTE</th><th>MONTO</th><th>ESTADO</th><th className="actions-column">ACCIONES</th></tr>
                </thead>
                <tbody>
                  {cargando ? (
                    <tr><td colSpan={4} className="empty-state">Cargando facturas...</td></tr>
                  ) : facturasFiltradas.length === 0 ? (
                    <tr><td colSpan={4} className="empty-state">
                      {error ? "No se pudo obtener la información." : "No hay facturas que coincidan con la búsqueda."}
                    </td></tr>
                  ) : facturasFiltradas.map((factura: Factura) => (
                    <tr key={factura.id}>
                      <td>
                        <div className="customer-cell">
                          <span className={`customer-avatar avatar-${factura.nombreCliente.charCodeAt(0) % 4}`}>
                            {factura.nombreCliente.split(" ").map((parte) => parte[0]).slice(0, 2).join("").toUpperCase()}
                          </span>
                          <span><strong>{factura.nombreCliente}</strong><small>{factura.correoCliente}</small></span>
                        </div>
                      </td>
                      <td className="amount">{formatoMoneda.format(factura.monto)}</td>
                      <td><EstadoBadge estado={factura.estado} /></td>
                      <td className="actions-column"><button className="row-action" aria-label={`Ver factura de ${factura.nombreCliente}`}>•••</button></td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
            <footer className="table-footer">
              <span>Mostrando <strong>{facturasFiltradas.length}</strong> de <strong>{resumen.total}</strong> facturas</span>
              <span className={`footer-note ${apiConectada ? "" : "offline"}`}>
                <span /> {apiConectada ? "Datos sincronizados con MongoDB" : "Datos no sincronizados"}
              </span>
            </footer>
          </section>
          <p className="page-footnote">Los correos se simulan y se registran en los logs del backend.</p>
        </div>
      </section>
    </main>
  );
}

export default App;
