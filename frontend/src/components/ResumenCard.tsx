import type { LucideIcon } from "lucide-react";

interface ResumenCardProps {
  titulo: string;
  valor: number;
  detalle: string;
  icono: LucideIcon;
  tono: string;
}

export function ResumenCard({ titulo, valor, detalle, icono: Icono, tono }: ResumenCardProps) {
  return (
    <article className="summary-card">
      <div className={`summary-icon ${tono}`}><Icono size={20} strokeWidth={1.8} /></div>
      <p className="summary-title">{titulo}</p>
      <div className="summary-value">{valor}</div>
      <p className="summary-detail">{detalle}</p>
    </article>
  );
}
