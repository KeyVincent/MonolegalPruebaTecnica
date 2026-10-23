interface EstadoBadgeProps {
  estado: string;
}

const etiquetas: Record<string, string> = {
  primerrecordatorio: "Primer recordatorio",
  segundorecordatorio: "Segundo recordatorio",
  desactivado: "Desactivado",
};

export function EstadoBadge({ estado }: EstadoBadgeProps) {
  const clase = estado.toLowerCase().replace(/\s/g, "-");
  const etiqueta = etiquetas[estado.toLowerCase()] ?? estado;

  return <span className={`status-badge status-${clase}`}>{etiqueta}</span>;
}
