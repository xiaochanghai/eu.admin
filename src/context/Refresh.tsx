import { createContext, useState } from "react";

interface RefreshContextType {
  outletShow: boolean;
  updateOutletShow: (val: boolean) => void;
  reload: boolean;
  updateReload: (val: boolean) => void;
}

export const RefreshContext = createContext<RefreshContextType>({
  outletShow: true,
  updateOutletShow: () => {},
  reload: false,
  updateReload: () => {}
});

export const RefreshProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [outletShow, setOutletShow] = useState(true);
  const [reload, setReload] = useState(true);

  const updateOutletShow = (val: boolean) => {
    setOutletShow(val);
  };
  const updateReload = (val: boolean) => {
    setReload(val);
  };

  const contextValue = {
    outletShow,
    updateOutletShow,
    reload,
    updateReload
  };

  return <RefreshContext.Provider value={contextValue}>{children}</RefreshContext.Provider>;
};
