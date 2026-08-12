export default function VerificationHistory() {
  return (
    <section
      style={{
        maxWidth: "1100px",
        margin: "0 auto",
        padding: "24px",
      }}
    >
      <header style={{ marginBottom: "24px" }}>
        <h1 style={{ marginBottom: "8px" }}>Verification History</h1>

        <p style={{ color: "#64748b", margin: 0 }}>
          View completed citizen verification records and their results.
        </p>
      </header>

      <div
        style={{
          padding: "24px",
          border: "1px solid #e2e8f0",
          borderRadius: "12px",
          background: "#ffffff",
        }}
      >
        <h2 style={{ marginTop: 0 }}>Verification Records</h2>

        <p style={{ color: "#64748b", marginBottom: 0 }}>
          No verification records are currently available. Records will appear
          here after the verification workflow is connected.
        </p>
      </div>
    </section>
  );
}
