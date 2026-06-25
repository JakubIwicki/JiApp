import { Nav } from "./components/Nav";
import { Hero } from "./sections/Hero";
import { Projects } from "./sections/Projects";
import { Download } from "./sections/Download";
import { Footer } from "./sections/Footer";

/**
 * Single-page portfolio app — no router, no SSR.
 *
 * Sections rendered in scroll order. The <Download /> section provides the
 * device-aware APK download experience (Android button, desktop QR, live
 * metadata from S3).
 */
export function App() {
  return (
    <>
      <Nav />
      <main>
        <Hero />
        <Projects />
        <Download />
      </main>
      <Footer />
    </>
  );
}
