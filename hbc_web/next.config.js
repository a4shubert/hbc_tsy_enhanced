/** @type {import('next').NextConfig} */
const nextConfig = {
  reactStrictMode: true,
  async rewrites() {
    const backend = (process.env.HBC_API_URL || "http://localhost:5047").replace(/\/$/, "")
    return [
      {
        source: "/backend/:path*",
        destination: `${backend}/:path*`,
      },
    ]
  },
}

module.exports = nextConfig
