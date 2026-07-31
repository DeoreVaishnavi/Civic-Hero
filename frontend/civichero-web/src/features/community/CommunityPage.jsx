import { useMemo, useState } from "react";
import { Box, Button, Card, CardContent, Chip, Container, InputAdornment, MenuItem, Skeleton, TextField, ToggleButton, ToggleButtonGroup, Typography } from "@mui/material";
import { Add, GridView, MapOutlined, Search, TrendingUp } from "@mui/icons-material";
import { useQuery } from "@tanstack/react-query";
import { Link } from "react-router";
import { complaintApi } from "../../api/complaintApi";
import ComplaintCard from "../complaints/components/ComplaintCard";
import ComplaintMarkerMap from "../../components/maps/ComplaintMarkerMap";
import PaginationControl from "../../components/common/PaginationControl";
import EmptyState from "../../components/common/EmptyState";
import useDebounce from "../../hooks/useDebounce";

export default function CommunityPage() {
  const [filters, setFilters] = useState({ page: 1, pageSize: 12, search: "", status: "", category: "", sortOrder: "desc" });
  const [view, setView] = useState("feed");
  const search = useDebounce(filters.search, 350);
  const params = useMemo(() => ({ ...filters, search }), [filters, search]);
  const { data = {}, isLoading } = useQuery({ queryKey: ["community", params], queryFn: () => complaintApi.list(params) });
  const { data: metadata = {} } = useQuery({ queryKey: ["complaint-metadata"], queryFn: complaintApi.metadata });
  const items = data.items || data.data || (Array.isArray(data) ? data : []);
  const totalPages = data.totalPages || Math.ceil((data.totalCount || items.length) / filters.pageSize) || 1;
  const change = (key, value) => setFilters((current) => ({ ...current, [key]: value, page: key === "page" ? value : 1 }));

  return <Container maxWidth="xl" sx={{ py: { xs: 4, md: 7 } }}><Box className="community-hero mb-7 overflow-hidden rounded-[32px] p-6 text-white md:p-10"><div className="relative z-10 max-w-3xl"><Chip icon={<TrendingUp />} label="Neighbourhood action, in one place" sx={{ bgcolor: "rgba(255,255,255,.16)", color: "white", mb: 2 }} /><Typography variant="h2">See what your community is solving.</Typography><Typography sx={{ mt: 2, maxWidth: 650, opacity: .85, fontSize: 18 }}>Discover civic issues, support reports that affect you, and join constructive public discussions.</Typography><Button component={Link} to="/report-complaint" variant="contained" color="secondary" size="large" startIcon={<Add />} sx={{ mt: 3 }}>Report an issue</Button></div></Box><Card sx={{ mb: 4 }}><CardContent className="grid gap-3 md:grid-cols-5"><TextField className="md:col-span-2" placeholder="Search by title, reference or place" value={filters.search} onChange={(event) => change("search", event.target.value)} InputProps={{ startAdornment: <InputAdornment position="start"><Search /></InputAdornment> }} /><TextField select label="Status" value={filters.status} onChange={(event) => change("status", event.target.value)}><MenuItem value="">All statuses</MenuItem>{["Created", "Assigned", "InProgress", "Resolved", "Closed"].map((status) => <MenuItem key={status} value={status}>{status.replace(/([a-z])([A-Z])/g, "$1 $2")}</MenuItem>)}</TextField><TextField select label="Category" value={filters.category} onChange={(event) => change("category", event.target.value)}><MenuItem value="">All categories</MenuItem>{(metadata.categories || []).map((category) => <MenuItem key={category.code || category.id} value={category.code || category.name}>{category.name}</MenuItem>)}</TextField><div className="flex items-center justify-end"><ToggleButtonGroup exclusive value={view} onChange={(_, next) => next && setView(next)}><ToggleButton value="feed" aria-label="Card feed"><GridView /></ToggleButton><ToggleButton value="map" aria-label="Map view"><MapOutlined /></ToggleButton></ToggleButtonGroup></div></CardContent></Card>{isLoading ? <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">{Array.from({ length: 6 }, (_, index) => <Skeleton key={index} variant="rounded" height={260} />)}</div> : !items.length ? <EmptyState title="No community issues match these filters" description="Try removing a filter or be the first to report an issue." /> : view === "map" ? <ComplaintMarkerMap complaints={items} /> : <div className="stagger-grid grid gap-5 md:grid-cols-2 xl:grid-cols-3">{items.map((complaint) => <ComplaintCard key={complaint.id} complaint={complaint} />)}</div>}<PaginationControl page={filters.page} count={totalPages} onChange={(page) => change("page", page)} /></Container>;
}
