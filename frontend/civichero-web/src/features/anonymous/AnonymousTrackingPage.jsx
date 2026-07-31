import { useState } from "react";
import { Alert, Box, Button, Card, CardContent, Container, TextField, Typography } from "@mui/material";
import { Search, ShieldOutlined } from "@mui/icons-material";
import { useMutation } from "@tanstack/react-query";
import { anonymousApi } from "../../api/anonymousApi";
import StatusChip from "../../components/common/StatusChip";
import StatusTimeline from "../complaints/components/StatusTimeline";
import { getApiError } from "../../utils/apiErrorHelpers";

export default function AnonymousTrackingPage() {
  const [referenceNumber, setReferenceNumber] = useState("");
  const [trackingToken, setTrackingToken] = useState("");
  const mutation = useMutation({ mutationFn: () => anonymousApi.track(referenceNumber.trim(), trackingToken.trim()) });
  const item = mutation.data;
  return <Container maxWidth="md" sx={{ py: { xs: 5, md: 9 } }}><Box className="mb-7 text-center reveal-up"><ShieldOutlined color="primary" sx={{ fontSize: 48 }} /><Typography variant="h2">Private complaint tracking</Typography><Typography color="text.secondary">Your token stays in this browser request and is never exposed publicly.</Typography></Box><Card><CardContent sx={{ p: { xs: 3, md: 5 } }}><div className="grid gap-3 md:grid-cols-[1fr_1.5fr_auto]"><TextField label="Reference number" placeholder="CH-2026-00124" value={referenceNumber} onChange={(event) => setReferenceNumber(event.target.value)} /><TextField label="Tracking token" type="password" value={trackingToken} onChange={(event) => setTrackingToken(event.target.value)} /><Button startIcon={<Search />} variant="contained" disabled={!referenceNumber.trim() || !trackingToken.trim() || mutation.isPending} onClick={() => mutation.mutate()}>Track</Button></div>{mutation.isError && <Alert severity="error" sx={{ mt: 3 }}>{getApiError(mutation.error, "Unable to find that complaint.")}</Alert>}</CardContent></Card>{item && <div className="mt-5 space-y-5 reveal-up"><Card><CardContent sx={{ p: 4 }}><div className="flex flex-wrap items-start justify-between gap-3"><div><Typography variant="overline">{item.referenceNumber}</Typography><Typography variant="h4">{item.title}</Typography><Typography color="text.secondary">{item.category} · {item.departmentName} · {item.wardName}</Typography></div><StatusChip status={item.status} /></div><Typography sx={{ mt: 3 }}>{item.address}</Typography>{item.possibleEmergency && <Alert severity="warning" sx={{ mt: 2 }}>Emergency review: {item.emergencyReviewStatus}</Alert>}</CardContent></Card><Card><CardContent sx={{ p: 4 }}><Typography variant="h5" sx={{ mb: 3 }}>Public progress timeline</Typography><StatusTimeline items={(item.timeline || []).map((entry) => ({ ...entry, status: entry.eventType, createdAt: entry.timestamp }))} /></CardContent></Card></div>}</Container>;
}
