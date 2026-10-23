export interface Factura {
  id: string;
  nombreCliente: string;
  correoCliente: string;
  monto: number;
  estado: string;
}

export interface ResumenFacturas {
  total: number;
  primerRecordatorio: number;
  segundoRecordatorio: number;
  desactivadas: number;
  otrosEstados: number;
  facturas: Factura[];
}

export interface ResultadoProcesamiento {
  procesadas: number;
  fallidas: number;
  errores: string[];
}

const apiUrl = (import.meta.env.VITE_API_URL ?? "http://localhost:5000").replace(/\/$/, "");

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`${apiUrl}${path}`, {
    ...init,
    headers: { "Content-Type": "application/json", ...init?.headers },
  });

  if (!response.ok) {
    const detail = await response.text();
    throw new Error(detail || `Error HTTP ${response.status}`);
  }

  return response.json() as Promise<T>;
}

export const facturasApi = {
  obtenerResumen: () => request<ResumenFacturas>("/api/facturas/resumen"),
  procesarRecordatorios: () =>
    request<ResultadoProcesamiento>("/api/recordatorios/procesar", { method: "POST" }),
};
