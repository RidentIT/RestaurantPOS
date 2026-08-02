import type { Config } from "tailwindcss";

/** Maps a CSS custom property holding raw HSL channels to a Tailwind colour. */
const hsl = (variable: string) => `hsl(var(--${variable}) / <alpha-value>)`;

const config: Config = {
  darkMode: ["class"],
  content: [
    "./index.html",
    "./src/app/**/*.{js,ts,jsx,tsx}",
    "./src/pages/**/*.{js,ts,jsx,tsx}",
    "./src/widgets/**/*.{js,ts,jsx,tsx}",
    "./src/features/**/*.{js,ts,jsx,tsx}",
    "./src/entities/**/*.{js,ts,jsx,tsx}",
    "./src/shared/**/*.{js,ts,jsx,tsx}",
  ],
  theme: {
    extend: {
      colors: {
        background: hsl("background"),
        foreground: hsl("foreground"),
        border: hsl("border"),
        input: hsl("input"),
        ring: hsl("ring"),
        card: {
          DEFAULT: hsl("card"),
          foreground: hsl("card-foreground"),
        },
        popover: {
          DEFAULT: hsl("popover"),
          foreground: hsl("popover-foreground"),
        },
        primary: {
          DEFAULT: hsl("primary"),
          foreground: hsl("primary-foreground"),
        },
        secondary: {
          DEFAULT: hsl("secondary"),
          foreground: hsl("secondary-foreground"),
        },
        muted: {
          DEFAULT: hsl("muted"),
          foreground: hsl("muted-foreground"),
        },
        accent: {
          DEFAULT: hsl("accent"),
          foreground: hsl("accent-foreground"),
        },
        destructive: {
          DEFAULT: hsl("destructive"),
          foreground: hsl("destructive-foreground"),
        },
        success: {
          DEFAULT: hsl("success"),
          foreground: hsl("success-foreground"),
        },
        warning: {
          DEFAULT: hsl("warning"),
          foreground: hsl("warning-foreground"),
        },
      },
      borderRadius: {
        lg: "var(--radius)",
        md: "calc(var(--radius) - 2px)",
        sm: "calc(var(--radius) - 4px)",
      },
      keyframes: {
        "fade-in": {
          from: { opacity: "0" },
          to: { opacity: "1" },
        },
        "slide-up": {
          from: { opacity: "0", transform: "translateY(6px)" },
          to: { opacity: "1", transform: "translateY(0)" },
        },
      },
      animation: {
        "fade-in": "fade-in 150ms ease-out",
        "slide-up": "slide-up 200ms ease-out",
      },
    },
  },
  plugins: [],
};

export default config;
