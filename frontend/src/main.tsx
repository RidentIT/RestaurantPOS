import React from 'react';
import ReactDOM from 'react-dom/client';
import { Providers } from '@/app/providers';
import App from '@/app/App';
import '@/app/globals.css';

ReactDOM.createRoot(document.getElementById('root') as HTMLElement).render(
  <React.StrictMode>
    <Providers>
      <App />
    </Providers>
  </React.StrictMode>
);
