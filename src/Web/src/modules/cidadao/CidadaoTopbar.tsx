// Topbar do Portal do Cidadao (landmark role=banner). NAO reusa app/shell/Header porque
// aquele depende do useAuth do ADMIN (lanca fora do AuthProvider do back-office). Reusa
// SOMENTE as classes gov.br ja existentes (.tg-topbar/.tg-brand) — sem editar styles.
// Marca + selo "Portal do Cidadao"; a identificacao/sair do cidadao fica no layout gated.
export function CidadaoTopbar() {
  return (
    <header className="tg-topbar" role="banner">
      <div className="tg-brand">
        <span className="tg-brand-mark" aria-hidden="true">
          TG
        </span>
        <span className="tg-brand-text">
          <span className="tg-brand-name">Tensorroot.Gov</span>
          <span className="tg-brand-sub">Portal do Cidadao</span>
        </span>
      </div>
    </header>
  );
}
