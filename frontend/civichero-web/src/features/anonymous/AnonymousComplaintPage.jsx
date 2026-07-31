import { useState } from "react";
import { Alert, Box, Button, Card, CardContent, Checkbox, Container, FormControlLabel, MenuItem, Step, StepLabel, Stepper, TextField, Typography } from "@mui/material";
import { ArrowBack, ArrowForward, ContentCopy, GppGood, VisibilityOff } from "@mui/icons-material";
import { useMutation, useQuery } from "@tanstack/react-query";
import { Link } from "react-router";
import { anonymousApi } from "../../api/anonymousApi";
import { complaintApi } from "../../api/complaintApi";
import ComplaintImageSection from "../complaints/components/ComplaintImageSection";
import ComplaintLocationSection from "../complaints/components/ComplaintLocationSection";
import { getApiError } from "../../utils/apiErrorHelpers";
import { getAnonymousComplaintCaptchaToken } from "../../utils/recaptcha";

const initial = { images: [], category: "", departmentId: "", wardId: "", title: "", description: "", address: "", latitude: null, longitude: null, locationSource: "MAP_SELECTION", locationConfirmed: false, contactEmail: "", contactPhone: "", consentToLimitedContactStorage: false, possibleEmergency: false, emergencyReason: "" };
const steps = ["Evidence", "Issue details", "Location", "Privacy & review"];

export default function AnonymousComplaintPage() {
  const [step, setStep] = useState(0);
  const [value, setValue] = useState(initial);
  const [error, setError] = useState("");
  const { data: metadata = {} } = useQuery({ queryKey: ["complaint-metadata"], queryFn: complaintApi.metadata });
  const mutation = useMutation({ mutationFn: anonymousApi.create });
  const set = (key, next) => setValue((current) => ({ ...current, [key]: next }));
  const wards = (metadata.wards || []).filter((ward) => !value.departmentId || ward.departmentId === Number(value.departmentId));

  const validate = () => {
    if (step === 0 && !value.images.length) return "Add at least one clear image.";
    if (step === 1 && (!value.category || !value.departmentId || !value.wardId || value.title.trim().length < 5 || value.description.trim().length < 20 || !value.address.trim())) return "Complete all required issue details.";
    if (step === 2 && (!value.locationConfirmed || value.latitude == null || value.longitude == null)) return "Select and confirm the issue location.";
    return "";
  };
  const next = () => { const issue = validate(); setError(issue); if (!issue) setStep((current) => current + 1); };
  const submit = async () => {
    setError("");
    try {
      if ((value.contactEmail.trim() || value.contactPhone.trim()) && !value.consentToLimitedContactStorage) {
        setError("Consent is required when optional contact details are provided.");
        return;
      }
      const captchaToken = await getAnonymousComplaintCaptchaToken();
      await mutation.mutateAsync({ ...value, captchaToken });
    } catch (exception) { setError(getApiError(exception, "Anonymous complaint submission failed.")); }
  };
  const created = mutation.data;

  if (created) return <Container maxWidth="md" sx={{ py: { xs: 6, md: 10 } }}><Card className="reveal-up overflow-hidden"><Box className="bg-gradient-to-br from-blueberry-600 to-blueberry-700 p-8 text-center text-white"><GppGood sx={{ fontSize: 62 }} /><Typography variant="h3" sx={{ mt: 1 }}>Complaint submitted privately</Typography><Typography sx={{ opacity: .85, mt: 1 }}>Your identity was not attached to this report.</Typography></Box><CardContent sx={{ p: { xs: 3, md: 5 } }} className="space-y-5 text-center"><Alert severity="warning">Save this tracking token now. It cannot be recovered later.</Alert><Typography color="text.secondary">Reference number</Typography><Typography variant="h4" fontWeight={900}>{created.referenceNumber}</Typography><Box className="rounded-2xl border border-dashed border-blueberry-600 bg-blueberry-50 p-4"><Typography variant="caption">PRIVATE TRACKING TOKEN</Typography><Typography sx={{ wordBreak: "break-all", fontFamily: "monospace", fontWeight: 800 }}>{created.trackingToken}</Typography><Button startIcon={<ContentCopy />} onClick={() => navigator.clipboard.writeText(created.trackingToken)}>Copy token</Button></Box><Button component={Link} to="/track-anonymous" variant="contained" size="large">Track this complaint</Button></CardContent></Card></Container>;

  return <Container maxWidth="lg" sx={{ py: { xs: 4, md: 8 } }}><Box className="mb-8 text-center reveal-up"><VisibilityOff color="primary" sx={{ fontSize: 44 }} /><Typography variant="h2">Report without revealing your identity</Typography><Typography color="text.secondary" sx={{ mt: 1 }}>Submit evidence securely and receive a private tracking token.</Typography></Box><Card><CardContent sx={{ p: { xs: 2, md: 5 } }}><Stepper activeStep={step} alternativeLabel sx={{ mb: 5 }}>{steps.map((label) => <Step key={label}><StepLabel>{label}</StepLabel></Step>)}</Stepper>{error && <Alert severity="error" sx={{ mb: 3 }}>{error}</Alert>}{step === 0 && <ComplaintImageSection files={value.images} onChange={(images) => set("images", images)} />}{step === 1 && <div className="grid gap-4 md:grid-cols-2"><TextField select required label="Category" value={value.category} onChange={(event) => set("category", event.target.value)}>{(metadata.categories || []).map((item) => <MenuItem key={item.code || item.id} value={item.code || item.name}>{item.name}</MenuItem>)}</TextField><TextField select required label="Department" value={value.departmentId} onChange={(event) => { setValue({ ...value, departmentId: event.target.value, wardId: "" }); }}>{(metadata.departments || []).map((item) => <MenuItem key={item.id} value={item.id}>{item.name}</MenuItem>)}</TextField><TextField select required label="Ward" value={value.wardId} onChange={(event) => set("wardId", event.target.value)}>{wards.map((item) => <MenuItem key={item.id} value={item.id}>{item.name}</MenuItem>)}</TextField><TextField required label="Short title" value={value.title} onChange={(event) => set("title", event.target.value)} inputProps={{ maxLength: 100 }} /><TextField className="md:col-span-2" required multiline minRows={4} label="Describe the issue" value={value.description} onChange={(event) => set("description", event.target.value)} /><TextField className="md:col-span-2" required label="Landmark or address" value={value.address} onChange={(event) => set("address", event.target.value)} /><FormControlLabel control={<Checkbox checked={value.possibleEmergency} onChange={(event) => set("possibleEmergency", event.target.checked)} />} label="This may present an immediate danger" />{value.possibleEmergency && <TextField label="Why may this be an emergency?" value={value.emergencyReason} onChange={(event) => set("emergencyReason", event.target.value)} />}</div>}{step === 2 && <ComplaintLocationSection value={value} onChange={setValue} />}{step === 3 && <div className="grid gap-5 md:grid-cols-2"><Box><Typography variant="h5">Privacy choices</Typography><Typography color="text.secondary" sx={{ my: 1 }}>Contact details are optional, encrypted, and never shown to operational staff.</Typography><TextField fullWidth sx={{ mt: 2 }} label="Optional email" type="email" value={value.contactEmail} onChange={(event) => set("contactEmail", event.target.value)} /><TextField fullWidth sx={{ mt: 2 }} label="Optional phone" value={value.contactPhone} onChange={(event) => set("contactPhone", event.target.value)} /><FormControlLabel sx={{ mt: 1 }} control={<Checkbox checked={value.consentToLimitedContactStorage} onChange={(event) => set("consentToLimitedContactStorage", event.target.checked)} />} label="I consent to encrypted storage of these optional contact details" /></Box><Box className="rounded-3xl bg-cream-200 p-5"><Typography variant="h5">Review</Typography><Typography fontWeight={800} sx={{ mt: 2 }}>{value.title}</Typography><Typography color="text.secondary">{value.category} · Ward {value.wardId}</Typography><Typography sx={{ mt: 2 }}>{value.description}</Typography><Typography variant="body2" sx={{ mt: 2 }}>{value.address}</Typography><Typography variant="body2">{value.images.length} image(s) · location confirmed</Typography></Box></div>}<Box className="mt-6 flex justify-between"><Button startIcon={<ArrowBack />} disabled={step === 0 || mutation.isPending} onClick={() => setStep((current) => current - 1)}>Back</Button>{step < 3 ? <Button endIcon={<ArrowForward />} variant="contained" onClick={next}>Continue</Button> : <Button variant="contained" size="large" disabled={mutation.isPending} onClick={submit}>{mutation.isPending ? "Submitting securely…" : "Submit anonymously"}</Button>}</Box></CardContent></Card></Container>;
}
