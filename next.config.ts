import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  /* config options here */
  env: {
    CUSTOM_KEY: process.env['CUSTOM_KEY'],
  },
  images: {
    domains: ['images.unsplash.com', 'via.placeholder.com'],
  },
  serverExternalPackages: ['@prisma/client'],
  experimental: {
    turbo: {
      rules: {
        '*.svg': {
          loaders: ['@svgr/webpack'],
          as: '*.js',
        },
      },
    },
  },
};

export default nextConfig;
