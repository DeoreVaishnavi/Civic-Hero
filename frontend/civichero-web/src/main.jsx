import React from "react";
import ReactDOM from "react-dom/client";
import App from "./App";
import AppProviders from "./providers/AppProviders";
import "./i18n/i18n";
import "./styles/index.css";
const root = document.getElementById("root");
if (!root) throw new Error("Root element with id 'root' was not found.");
ReactDOM.createRoot(root).render(<React.StrictMode><AppProviders><App /></AppProviders></React.StrictMode>);
