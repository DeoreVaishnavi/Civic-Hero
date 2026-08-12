export default function AppealManagement() {
  return (
    <section
      style={{
        maxWidth: "1100px",
        margin: "0 auto",
        padding: "24px",
      }}
    >
      <header style={{ marginBottom: "24px" }}>
        <h1 style={{ marginBottom: "8px" }}>Appeal Management</h1>

        <p style={{ color: "#64748b" }}>
          Review and manage citizen appeals related to complaint decisions.
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
        <h2 style={{ marginTop: 0 }}>Appeals Queue</h2>

        <p style={{ color: "#64748b", marginBottom: 0 }}>
          No appeals are currently available. Appeal records will appear here
          when the appeal workflow is connected.
        </p>
      </div>
    </section>
  );
}
