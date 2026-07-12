import { createContext, useContext, type ReactNode, useMemo } from "react";
import { MetadataService } from "./MetadataService";

export interface ServicesBag {
  readonly metadataService: MetadataService;
}

const ServiceContext = createContext<ServicesBag>({
  metadataService: new MetadataService(),
});

export interface ServiceProviderProps {
  readonly children: ReactNode;
  readonly services?: ServicesBag;
}

export function ServiceProvider({ children, services }: ServiceProviderProps) {
  const bag = useMemo<ServicesBag>(
    () => services ?? { metadataService: new MetadataService() },
    [services],
  );
  return (
    <ServiceContext.Provider value={bag}>{children}</ServiceContext.Provider>
  );
}

export function useServices(): ServicesBag {
  return useContext(ServiceContext);
}
