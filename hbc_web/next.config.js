/** @type {import('next').NextConfig} */
const nextConfig = {
  reactStrictMode: true,
  async rewrites() {
    const apiUrl = process.env.HBC_API_URL
    if (!apiUrl) return []
    const backend = apiUrl.replace(/\/$/, "")
    return [
      {
        source: "/backend/:path*",
        destination: `${backend}/:path*`,
      },
    ]
  },
}

module.exports = nextConfig
