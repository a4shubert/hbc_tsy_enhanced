export function Footer() {
  return (
    <footer
      className="mt-auto w-full py-4 text-center text-sm"
      style={{
        backgroundColor: "var(--color-card)",
        color: "var(--color-muted)",
      }}
    >
      Copyright © {new Date().getFullYear()} HBC, London
    </footer>
  );
}
