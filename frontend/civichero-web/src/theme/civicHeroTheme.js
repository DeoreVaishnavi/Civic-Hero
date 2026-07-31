import { alpha, createTheme } from "@mui/material/styles";

const civicHeroTheme = createTheme({
  palette: { mode: "light", primary: { main: "#3346A8", dark: "#263782", light: "#EEF0FF", contrastText: "#FFFFFF" }, secondary: { main: "#F2B84B", dark: "#D69825", light: "#FFF1CF", contrastText: "#172033" }, background: { default: "#FFF8E8", paper: "#FFFFFF" }, text: { primary: "#172033", secondary: "#667085" }, divider: "#E4E7EC", success: { main: "#238636" }, warning: { main: "#D97706" }, error: { main: "#D92D20" }, info: { main: "#1570EF" } },
  shape: { borderRadius: 16 },
  typography: { fontFamily: '"Inter", "Segoe UI", sans-serif', h1: { fontWeight: 900, letterSpacing: "-.045em", lineHeight: 1.02, fontSize: "clamp(3.2rem, 6vw, 6.3rem)" }, h2: { fontWeight: 900, letterSpacing: "-.035em", lineHeight: 1.08, fontSize: "clamp(2.25rem, 4vw, 4rem)" }, h3: { fontWeight: 850, letterSpacing: "-.025em" }, h4: { fontWeight: 850, letterSpacing: "-.02em" }, h5: { fontWeight: 800 }, h6: { fontWeight: 700 }, overline: { fontWeight: 900, letterSpacing: ".13em" }, button: { fontWeight: 800, textTransform: "none", letterSpacing: "-.01em" } },
  components: {
    MuiButton: { defaultProps: { disableElevation: true }, styleOverrides: { root: { borderRadius: 12, minHeight: 44, paddingInline: 18, transition: "transform .2s ease, box-shadow .2s ease, background-color .2s ease", "&:hover": { transform: "translateY(-2px)" } }, containedPrimary: { boxShadow: "0 10px 24px rgba(51,70,168,.24)", "&:hover": { boxShadow: "0 14px 30px rgba(51,70,168,.32)" } } } },
    MuiCard: { styleOverrides: { root: { border: "1px solid #E4E7EC", borderRadius: 22, boxShadow: "0 10px 35px rgba(23,32,51,.07)", transition: "transform .25s ease, box-shadow .25s ease", backgroundImage: "none" } } },
    MuiTextField: { defaultProps: { size: "small" } },
    MuiOutlinedInput: { styleOverrides: { root: { borderRadius: 12, backgroundColor: "#fff", transition: "box-shadow .2s ease", "&.Mui-focused": { boxShadow: `0 0 0 4px ${alpha("#3346A8", .1)}` } } } },
    MuiChip: { styleOverrides: { root: { fontWeight: 750, borderRadius: 10 } } },
    MuiDialog: { defaultProps: { fullWidth: true }, styleOverrides: { paper: { borderRadius: 24 } } },
    MuiAppBar: { styleOverrides: { root: { backgroundImage: "linear-gradient(110deg,#263782,#3346A8)", boxShadow: "0 8px 28px rgba(38,55,130,.2)" } } },
    MuiLinearProgress: { styleOverrides: { root: { backgroundColor: "#EEF0FF" }, bar: { borderRadius: 10 } } },
  },
});
export default civicHeroTheme;
