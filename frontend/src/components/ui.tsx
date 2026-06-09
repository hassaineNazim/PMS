import type { ReactNode } from 'react';

export function PageHeader({ title, children }: { title: string; children?: ReactNode }) {
  return (
    <div className="topbar">
      <h1>{title}</h1>
      <div>{children}</div>
    </div>
  );
}

export function Modal({ title, onClose, children }: { title: string; onClose: () => void; children: ReactNode }) {
  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal" onClick={(e) => e.stopPropagation()}>
        <h2>{title}</h2>
        {children}
      </div>
    </div>
  );
}

const statusColors: Record<string, string> = {
  Available: 'green', Occupied: 'blue', Dirty: 'amber', OutOfService: 'red',
  Confirmed: 'blue', CheckedIn: 'green', CheckedOut: 'gray', Cancelled: 'red', NoShow: 'red',
  Paid: 'green', Pending: 'amber', Draft: 'gray', Refunded: 'amber',
  Active: 'green', Inactive: 'gray', OnLeave: 'amber',
};

export function Badge({ value }: { value: string }) {
  return <span className={`badge ${statusColors[value] ?? 'gray'}`}>{value}</span>;
}

export function money(amount: number, currency = 'DZD') {
  return `${amount.toLocaleString('fr-FR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })} ${currency}`;
}
