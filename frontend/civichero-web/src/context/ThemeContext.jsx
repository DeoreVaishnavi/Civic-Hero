import { createContext, useContext, useMemo, useState } from "react";
import PropTypes from "prop-types";
const ThemeModeContext = createContext(null);
export function ThemeContextProvider({ children }) { const [mode, setMode] = useState("light"); const value = useMemo(() => ({ mode, toggleMode: () => setMode((v) => v === "light" ? "dark" : "light") }), [mode]); return <ThemeModeContext.Provider value={value}>{children}</ThemeModeContext.Provider>; }
ThemeContextProvider.propTypes = { children: PropTypes.node.isRequired };
export const useThemeMode = () => useContext(ThemeModeContext);
