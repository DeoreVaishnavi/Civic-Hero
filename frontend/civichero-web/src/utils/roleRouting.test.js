import { describe, expect, it } from 'vitest';
import { dashboardForRole, profileForRole } from './roleRouting.js';

describe('role routing', () => {
  it.each([
    ['Citizen', '/citizen'],
    ['Officer', '/officer'],
    ['Supervisor', '/supervisor'],
    ['Admin', '/admin'],
    ['SuperAdmin', '/admin'],
  ])('routes %s to %s', (role, expected) => {
    expect(dashboardForRole(role)).toBe(expected);
  });

  it('uses the citizen portal as the safe default', () => {
    expect(dashboardForRole(undefined)).toBe('/citizen');
    expect(dashboardForRole('unexpected')).toBe('/citizen');
  });

  it('builds a role-specific profile route', () => {
    expect(profileForRole('Officer')).toBe('/officer/profile');
  });
});
