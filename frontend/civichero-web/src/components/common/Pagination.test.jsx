import { fireEvent, render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import Pagination from './Pagination.jsx';

describe('Pagination', () => {
  it('hides when only one page exists', () => {
    const { container } = render(<Pagination page={1} totalPages={1} onPageChange={() => {}} />);
    expect(container).toBeEmptyDOMElement();
  });

  it('moves to the next page', () => {
    const onPageChange = vi.fn();
    render(<Pagination page={2} totalPages={4} onPageChange={onPageChange} />);
    fireEvent.click(screen.getByRole('button', { name: 'Next' }));
    expect(onPageChange).toHaveBeenCalledWith(3);
  });

  it('disables previous on the first page', () => {
    render(<Pagination page={1} totalPages={4} onPageChange={() => {}} />);
    expect(screen.getByRole('button', { name: 'Previous' })).toBeDisabled();
  });
});
