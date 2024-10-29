import React, { useRef, useState, useEffect, useMemo } from "react";
import { Empty, Input, InputRef, Modal } from "antd";
import { EnterOutlined, SearchOutlined } from "@ant-design/icons";
import { useNavigate } from "react-router-dom";
import { RouteObjectType } from "@/routers/interface";
import { RootState, useSelector } from "@/redux";
import { useDebounce } from "ahooks";
import { Icon } from "@/components/Icon";

const SearchMenu: React.FC = () => {
  const navigate = useNavigate();

  const flatMenuList = useSelector((state: RootState) => state.auth.flatMenuList);

  const inputRef = useRef<InputRef>(null);
  const menuListRef = useRef<HTMLDivElement>(null);

  const [isModalOpen, setIsModalOpen] = useState(false);
  const [activePath, setActivePath] = useState("");
  const [searchValue, setSearchValue] = useState("");

  const debouncedSearchValue = useDebounce(searchValue, { wait: 300 });

  const showModal = () => setIsModalOpen(true);
  const closeModal = () => setIsModalOpen(false);

  const handleInputChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    setSearchValue(event.target.value);
  };

  const searchList = useMemo(() => {
    return debouncedSearchValue
      ? flatMenuList.filter(
          item =>
            (item.path!.toLowerCase().includes(debouncedSearchValue.toLowerCase()) ||
              item.meta!.title!.toLowerCase().includes(debouncedSearchValue.toLowerCase())) &&
            !item.meta?.isHide
        )
      : [];
  }, [debouncedSearchValue]);

  useEffect(() => {
    searchList.length ? setActivePath(searchList[0].path!) : setActivePath("");
  }, [searchList]);

  const mouseoverMenuItem = (item: RouteObjectType) => {
    setActivePath(item.path!);
  };

  const keyPressUpOrDown = (direction: number) => {
    const { length } = searchList;
    if (length === 0) return;
    const index = searchList.findIndex(item => item.path === activePath);
    const newIndex = (index + direction + length) % length;
    setActivePath(searchList[newIndex].path!);
    if (menuListRef.current?.firstElementChild) {
      const menuItemHeight = menuListRef.current.firstElementChild.clientHeight + 12 || 0;
      menuListRef.current.scrollTop = newIndex * menuItemHeight;
    }
  };

  const selectMenuItem = () => {
    const menu = searchList.find(item => item.path === activePath);
    if (!menu) return;
    if (menu.meta?.isLink) window.open(menu.meta.isLink, "_blank");
    navigate(menu.path!);
    closeModal();
  };

  const keyboardOperation = (event: KeyboardEvent) => {
    if (event.key === "ArrowUp") {
      event.preventDefault();
      keyPressUpOrDown(-1);
    } else if (event.key === "ArrowDown") {
      event.preventDefault();
      keyPressUpOrDown(1);
    } else if (event.key === "Enter") {
      event.preventDefault();
      selectMenuItem();
    }
  };

  useEffect(() => {
    const handler = isModalOpen ? window.addEventListener : window.removeEventListener;
    handler("keydown", keyboardOperation);
    return () => window.removeEventListener("keydown", keyboardOperation);
  }, [isModalOpen, keyboardOperation]);

  useEffect(() => {
    if (isModalOpen) {
      setTimeout(() => inputRef.current?.focus({ cursor: "start" }), 10);
    } else {
      setSearchValue("");
    }
  }, [isModalOpen]);

  return (
    <React.Fragment>
      <i className="iconfont icon-sousuo" onClick={showModal}></i>
      <Modal className="search-modal" width={600} footer={null} closable={false} open={isModalOpen} onCancel={closeModal}>
        <Input
          ref={inputRef}
          placeholder="菜单搜索：支持菜单名称、路径"
          size="large"
          prefix={<SearchOutlined style={{ fontSize: "18px" }} />}
          allowClear={true}
          value={searchValue}
          onChange={handleInputChange}
        />
        {searchList.length ? (
          <div className="menu-list" ref={menuListRef}>
            {searchList.map(item => (
              <div
                key={item.path}
                className={`menu-item ${item.path === activePath && "menu-active"}`}
                onMouseEnter={() => mouseoverMenuItem(item)}
                onClick={() => selectMenuItem()}
              >
                <Icon className="menu-icon" name={item.meta!.icon!} />
                <span className="menu-title">{item.meta?.title}</span>
                <EnterOutlined className="menu-enter" />
              </div>
            ))}
          </div>
        ) : (
          <Empty className="mt40 mb30" description="暂无菜单" />
        )}
      </Modal>
    </React.Fragment>
  );
};
export default SearchMenu;
