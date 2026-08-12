import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import StatusBadge from './StatusBadge.jsx';

describe('StatusBadge', () => {
  it('formats camel-case complaint states', () => {
    render(<StatusBadge status="VerificationPending" />);
    expect(screen.getByText('Verification Pending')).toBeInTheDocument();
  });

  it('renders unknown status without crashing', () => {
    render(<StatusBadge status="CustomState" />);
    expect(screen.getByText('Custom State')).toBeInTheDocument();
  });
});
