import { useState } from "react";
import { Button, Container, IconButton, Menu, MenuItem, Toolbar, Typography } from "@mui/material";
import { Menu as MenuIcon, VolunteerActivism } from "@mui/icons-material";
import { Link, Outlet } from "react-router";
import { useTranslation } from "react-i18next";
import LanguageSelector from "../common/LanguageSelector";
import Footer from "./Footer";

export default function PublicLayout() {
  const { t } = useTranslation();
  const [anchor, setAnchor] = useState(null);
  const links = [["/community", "Community"], ["/report-anonymously", "Anonymous report"], ["/track-anonymous", "Track private report"], ["/about", t("about")], ["/help", t("help")]];
  return <div className="flex min-h-screen flex-col"><header className="public-nav sticky top-0 z-[1200] border-b border-white/60 bg-white/80 backdrop-blur-xl"><Container maxWidth="xl"><Toolbar disableGutters className="gap-2"><Typography component={Link} to="/" variant="h5" color="primary" className="flex grow items-center gap-2 no-underline" fontWeight={900}><span className="logo-mark"><VolunteerActivism fontSize="small" /></span>CivicHero</Typography><div className="hidden items-center lg:flex">{links.map(([path,label]) => <Button key={path} component={Link} to={path}>{label}</Button>)}</div><div className="hidden sm:block"><LanguageSelector /></div><Button component={Link} to="/login">{t("login")}</Button><Button component={Link} to="/register" variant="contained">{t("register")}</Button><IconButton className="lg:!hidden" onClick={(event) => setAnchor(event.currentTarget)}><MenuIcon /></IconButton><Menu anchorEl={anchor} open={Boolean(anchor)} onClose={() => setAnchor(null)}>{links.map(([path,label]) => <MenuItem key={path} component={Link} to={path} onClick={() => setAnchor(null)}>{label}</MenuItem>)}</Menu></Toolbar></Container></header><main className="grow"><Outlet /></main><Footer /></div>;
}
