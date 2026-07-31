import PropTypes from "prop-types";
import { CssBaseline, GlobalStyles } from "@mui/material";
import { StyledEngineProvider, ThemeProvider } from "@mui/material/styles";
import { QueryClientProvider } from "@tanstack/react-query";
import { BrowserRouter } from "react-router";
import queryClient from "../config/queryClient";
import { AuthProvider } from "../context/AuthContext";
import { ThemeContextProvider } from "../context/ThemeContext";
import civicHeroTheme from "../theme/civicHeroTheme";
import ToastHost from "../components/common/ToastHost";
import ChatbotWidget from "../features/chatbot/ChatbotWidget";

export default function AppProviders({ children }) { return <StyledEngineProvider enableCssLayer><GlobalStyles styles="@layer theme, base, mui, components, utilities;" /><ThemeContextProvider><ThemeProvider theme={civicHeroTheme}><CssBaseline /><QueryClientProvider client={queryClient}><BrowserRouter><AuthProvider>{children}<ToastHost/><ChatbotWidget/></AuthProvider></BrowserRouter></QueryClientProvider></ThemeProvider></ThemeContextProvider></StyledEngineProvider>; }
AppProviders.propTypes = { children: PropTypes.node.isRequired };
