// ? Not yet used, currently using CSS solution for resolution
import { RootState, useSelector } from "@/redux";
import { useEffect, useState } from "react";

/**
 * @description Get the visible height of the first table
 * @param extraHeight Additional height (height of the content at the bottom of the table, number type, default is 105)
 * @param id Specify the table's id when there are multiple tables on the current page
 */
const useTableScroll = ({ extraHeight = 105, id }: { extraHeight?: number; id?: string } = {}) => {
  const tabs = useSelector((state: RootState) => state.global.tabs);

  const [scrollY, setScrollY] = useState("");

  useEffect(() => {
    let tHeader = null;
    if (id) {
      tHeader = document.getElementById(id)?.getElementsByClassName("ant-table-thead")[0] || null;
    } else {
      tHeader = document.getElementsByClassName("ant-table-thead")[0] || null;
    }

    // Distance between the table content and the top
    let tHeaderBottom = 0;

    tHeader && (tHeaderBottom = tHeader.getBoundingClientRect().bottom);

    if (!tabs) extraHeight - 40;

    // Window height - Table content top height - Table content bottom height
    setScrollY(`calc(100vh - ${tHeaderBottom + extraHeight}px)`);
  }, [tabs]);

  return { scrollY };
};

export default useTableScroll;
