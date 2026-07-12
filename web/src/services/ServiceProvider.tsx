import { createContext, useContext, type ReactNode, useMemo } from "react";
import { MetadataService } from "./MetadataService";

export interface ServicesBag {
  readonly metadataService: MetadataService;
}

const ServiceContext = createContext<ServicesBag>({
  metadataService: new MetadataService(),
});

export function ServiceProvider({
  children,
}: {
  readonly children: ReactNode;
}) {
  const services = useMemo<ServicesBag>(
    () => ({ metadataService: new MetadataService() }),
    [],
  );
  return (
    <ServiceContext.Provider value={services}>
      {children}
    </ServiceContext.Provider>
  );
}

export function useServices(): ServicesBag {
  return useContext(ServiceContext);
}
