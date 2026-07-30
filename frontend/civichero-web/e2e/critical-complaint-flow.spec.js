import { expect, test } from '@playwright/test';

const baseUrl = process.env.CIVICHERO_BASE_URL || 'http://localhost:8088';
const required = (name) => {
  const value = process.env[name];
  if (!value) throw new Error(`Missing required environment variable: ${name}`);
  return value;
};
const bearer = (token) => ({ Authorization: `Bearer ${token}` });
const dataOf = async (response, label) => {
  const body = await response.json().catch(() => ({}));
  expect(response.ok(), `${label}: ${response.status()} ${JSON.stringify(body)}`).toBeTruthy();
  return body.data ?? body;
};
const login = async (request, prefix) => dataOf(await request.post(`${baseUrl}/api/v1/auth/login`, {
  data: {
    email: required(`CIVICHERO_${prefix}_EMAIL`),
    password: required(`CIVICHERO_${prefix}_PASSWORD`),
    twoFactorCode: process.env[`CIVICHERO_${prefix}_TWO_FACTOR_CODE`] || null,
  },
}), `${prefix} login`);

// This test intentionally fails when any critical workflow endpoint, account, role scope,
// cloud dependency or reward transition is unavailable. Run it only against a dedicated
// staging/test environment, never against production data.
test('critical complaint journey reaches closure and reward', async ({ request, page }) => {
  test.setTimeout(180_000);
  const suffix = `${Date.now()}-${Math.floor(Math.random() * 10000)}`;
  const citizenEmail = `phase18.${suffix}@example.test`;
  const citizenPassword = `CivicHero!${suffix}Aa9`;
  const latitude = Number(process.env.CIVICHERO_E2E_LATITUDE || '19.0760');
  const longitude = Number(process.env.CIVICHERO_E2E_LONGITUDE || '72.8777');
  const departmentId = Number(required('CIVICHERO_E2E_DEPARTMENT_ID'));
  const wardId = Number(required('CIVICHERO_E2E_WARD_ID'));
  const officerId = Number(required('CIVICHERO_E2E_OFFICER_ID'));

  const registration = await dataOf(await request.post(`${baseUrl}/api/v1/auth/register`, {
    data: { fullName: `Phase 18 Citizen ${suffix}`, email: citizenEmail, phone: '9999999999', password: citizenPassword, confirmPassword: citizenPassword },
  }), 'register citizen');
  const verificationToken = registration.developmentVerificationToken || process.env.CIVICHERO_E2E_VERIFICATION_TOKEN;
  expect(verificationToken, 'Dedicated E2E environment must expose or supply the email verification token.').toBeTruthy();

  const citizen = await dataOf(await request.post(`${baseUrl}/api/v1/auth/verify-email`, {
    data: { email: citizenEmail, token: verificationToken },
  }), 'verify citizen email');
  const citizenToken = citizen.accessToken;
  expect(citizenToken).toBeTruthy();

  const pixel = Buffer.from('iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAusB9Y9Z7FQAAAAASUVORK5CYII=', 'base64');
  const created = await dataOf(await request.post(`${baseUrl}/api/v1/complaints`, {
    headers: bearer(citizenToken),
    multipart: {
      title: `Release gate road damage ${suffix}`,
      description: 'A deep pothole is creating a safety risk. This is an automated Phase 18 release-candidate test.',
      category: 'Roads', departmentId: String(departmentId), wardId: String(wardId),
      latitude: String(latitude), longitude: String(longitude), address: 'Phase 18 automated test location',
      images: { name: 'complaint.png', mimeType: 'image/png', buffer: pixel },
    },
  }), 'create complaint with evidence');
  const complaintId = created.complaint?.id ?? created.id;
  expect(complaintId).toBeTruthy();

  const supervisor = await login(request, 'SUPERVISOR');
  await dataOf(await request.post(`${baseUrl}/api/v1/ai/complaints/${complaintId}/analyze?force=true`, {
    headers: bearer(supervisor.accessToken),
  }), 'AI triage');
  await dataOf(await request.post(`${baseUrl}/api/v1/assignments`, {
    headers: bearer(supervisor.accessToken),
    data: { complaintId, officerId, reason: 'Phase 18 critical E2E assignment' },
  }), 'assign officer');

  const officer = await login(request, 'OFFICER');
  await dataOf(await request.post(`${baseUrl}/api/v1/assignments/${complaintId}/accept`, { headers: bearer(officer.accessToken) }), 'accept assignment');
  await dataOf(await request.post(`${baseUrl}/api/v1/assignments/${complaintId}/progress`, {
    headers: bearer(officer.accessToken),
    data: { message: 'Automated site inspection completed.', progressPercent: 60, latitude, longitude },
  }), 'record progress');
  await dataOf(await request.post(`${baseUrl}/api/v1/assignments/${complaintId}/complete`, {
    headers: bearer(officer.accessToken),
    multipart: { notes: 'Automated repair completed successfully.', latitude: String(latitude), longitude: String(longitude), evidence: { name: 'resolution.png', mimeType: 'image/png', buffer: pixel } },
  }), 'submit resolution');

  await dataOf(await request.post(`${baseUrl}/api/v1/verifications/geo-verify`, {
    headers: bearer(citizenToken), data: { complaintId, latitude, longitude },
  }), 'geo verification');
  await dataOf(await request.post(`${baseUrl}/api/v1/verifications`, {
    headers: bearer(citizenToken), data: { complaintId, approved: true, rating: 5, feedback: 'Automated release test approval.', latitude, longitude },
  }), 'approve resolution');

  const detail = await dataOf(await request.get(`${baseUrl}/api/v1/complaints/${complaintId}`, { headers: bearer(citizenToken) }), 'load closed complaint');
  const status = detail.complaint?.status ?? detail.status;
  expect(['Closed', 'CLOSED', 'ClosedAuto', 'CLOSED_AUTO']).toContain(status);

  let points;
  for (let attempt = 0; attempt < 12; attempt += 1) {
    points = await dataOf(await request.get(`${baseUrl}/api/v1/rewards/points`, { headers: bearer(citizenToken) }), 'load reward points');
    const lifetime = Number(points.lifetimeEarned ?? points.totalEarned ?? points.points ?? 0);
    if (lifetime > 0) break;
    await new Promise((resolve) => setTimeout(resolve, 5000));
  }
  expect(Number(points.lifetimeEarned ?? points.totalEarned ?? points.points ?? 0)).toBeGreaterThan(0);

  await page.goto(`${baseUrl}/login`);
  await expect(page.getByText(/CivicHero/i).first()).toBeVisible();
});
