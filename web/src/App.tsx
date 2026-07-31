import { Nav } from "./components/Nav";
import { Hero } from "./sections/Hero";
import { Skills } from "./sections/Skills";
import { Projects } from "./sections/Projects";
import { Download } from "./sections/Download";
import { Footer } from "./sections/Footer";

/**
 * Single-page portfolio app — no router, no SSR.
 *
 * Sections rendered in scroll order: About (Hero), Skills, Projects,
 * Download, Footer. The <Download /> section provides the device-aware
 * APK download experience (Android button, desktop QR, live metadata
 * from S3).
 */
export function App() {
  return (
    <>
      <Nav />
      <main>
        <Hero />
        <Skills />
        <Projects />
        <Download />
      </main>
      <Footer />
    </>
  );
}
