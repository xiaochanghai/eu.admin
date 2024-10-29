import React, { Suspense } from "react";
import { PageLoader } from "../Loading";

/**
 * @description Routing lazy loading
 * @param {Element} Comp Components that need to be accessed
 * @returns React.ReactNode
 */
const LazyComponent = (Comp: React.LazyExoticComponent<React.ComponentType>) => {
  return (
    <Suspense fallback={<PageLoader />}>
      <Comp />
    </Suspense>
  );
};

export default LazyComponent;
