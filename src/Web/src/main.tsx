import React from 'react';
import ReactDOM from 'react-dom/client';

// Design System gov.br — fonte única de tokens, componentes e estilos.
import '@govbr-ds/core/dist/core.min.css';
// Ícones Font Awesome (o gov.br DS referencia, mas não empacota, a webfont).
import '@fortawesome/fontawesome-free/css/all.min.css';
// Estilos de layout do shell.
import './styles/layout.css';

import { App } from './App';

const rootElement = document.getElementById('root');
if (!rootElement) {
  throw new Error('Elemento #root não encontrado.');
}

ReactDOM.createRoot(rootElement).render(
  <React.StrictMode>
    <App />
  </React.StrictMode>,
);
