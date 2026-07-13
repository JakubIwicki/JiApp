import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { ServiceProvider } from "./services/ServiceProvider";
import { ErrorBoundary } from "./components/ErrorBoundary";
import { App } from "./App";
import "./styles/global.css";

const root = document.getElementById("root");
if (!root) throw new Error("Root element #root not found in index.html");

createRoot(root).render(
  <StrictMode>
    <ErrorBoundary>
      <ServiceProvider>
        <App />
      </ServiceProvider>
    </ErrorBoundary>
  </StrictMode>,
);
