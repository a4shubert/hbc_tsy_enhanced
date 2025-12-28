export function Footer() {
  return (
    <footer className="w-full border-0 border-red-500 [background:var(--color-bg)] py-4 text-center text-lg text-[color:var(--color-muted)]">
      Copyright © {new Date().getFullYear()} HBC, London
    </footer>
  )
}
