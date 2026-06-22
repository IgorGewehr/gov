// Footer gov.br (br-footer). landmark role=contentinfo.
export function Footer() {
  return (
    <footer className="br-footer" role="contentinfo">
      <div className="container-lg">
        <div className="d-flex flex-column flex-sm-row justify-content-between py-3">
          <span className="text-down-01">Tensorroot.Gov — sistema de missão crítica para prefeituras e câmaras.</span>
          <span className="text-down-01 text-gray-60">&copy; {new Date().getFullYear()} Tensorroot.Gov</span>
        </div>
      </div>
    </footer>
  );
}
