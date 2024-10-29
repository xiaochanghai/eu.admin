import { useState } from "react";
import { useInterval } from "ahooks";
import dayjs from "dayjs";

const HeaderTime = () => {
  const [time, setTime] = useState(dayjs().format("YYYY年MM月DD HH:mm:ss"));

  useInterval(() => {
    setTime(dayjs().format("YYYY年MM月DD HH:mm:ss"));
  }, 1000);

  return time;
};

export default HeaderTime;
