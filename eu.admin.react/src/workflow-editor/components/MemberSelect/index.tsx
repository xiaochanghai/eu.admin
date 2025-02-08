import { Avatar, Modal, Tooltip } from "antd";
import { ISelect } from "@/dsl/component";
import { NodeUserInfo } from "../..//classes/vlife";
import FlowSelect, { FlowSelectProps } from "./FlowSelect";
import { list } from "@/api/SysDept";
import { list as userList } from "@/api/SysUser";
// import { listAll as groupList } from "@/api/SysGroup";
import { useEffect, useState } from "react";
import { Icon } from "@/components";

interface MemberSelectProps extends FlowSelectProps {
  label?: string;
}
// const color = [
//   "blue",
//   "cyan",
//   "green",
//   "grey",
//   "indigo",
//   "light-blue",
//   "light-green",
//   "lime",
//   "amber",
//   "orange",
//   "pink",
//   "purple",
//   "red",
//   "teal",
//   "violet",
//   "yellow"
// ];
/**
 * 用户选择器
 * 根据部门/角色进行筛选
 */
export default ({ value, label, read, multiple, showUser, onDataChange }: MemberSelectProps) => {
  const windowSize = {
    width: window.innerWidth,
    height: window.innerHeight
  };
  const [modalState, setModalState] = useState(false); //模态窗口状态

  const [modalSelected, setModalSelected] = useState<Partial<NodeUserInfo>[]>(value || []);
  const [deptSelectData, setDeptSelectData] = useState<ISelect[]>(); //部门数据
  const [userSelectData, setUserSelectData] = useState<ISelect[]>(); //用户数据
  // const [groupSelectData, setGroupSelectData] = useState<ISelect[]>(); //用户数据

  useEffect(() => {
    userList().then(d => {
      setUserSelectData(
        d?.map(r => {
          return { label: r.UserName, value: showUser ? r.ID : r.UserName };
        }) || []
      );
    });
    list().then(d => {
      setDeptSelectData(
        d?.map((r: any) => {
          return { label: r.DepartmentName, value: r.ID };
        }) || []
      );
    });

    // groupList().then(d => {
    //   setGroupSelectData(
    //     d.data?.map(r => {
    //       return { label: r.name, value: r.id };
    //     }) || []
    //   );
    // });
  }, []);

  useEffect(() => {
    setModalSelected(value || []);
  }, [value]);
  //首次数据加载
  return (
    <div>
      <Modal
        height={windowSize.height / 2}
        title={`${label || "请"}选择`}
        open={modalState}
        onOk={() => {
          setModalState(false);
          onDataChange?.(modalSelected);
        }}
        // className="!overflow-y-auto"
        centered
        onCancel={() => {
          setModalState(false);
          setModalSelected(value || []);
        }}
        width={900}
      >
        <FlowSelect
          className=" bg-slate-400"
          userSelectData={userSelectData}
          deptSelectData={deptSelectData}
          // groupSelectData={groupSelectData}
          value={modalSelected}
          multiple={multiple}
          showUser={showUser}
          onDataChange={(d?: Partial<NodeUserInfo>[]) => {
            setModalSelected(d ? d : []);
          }}
        />
      </Modal>
      <div className=" flex items-center">
        {value?.map((d, index) => {
          return (
            <div key={`${index}_selected`} className="relative group">
              <Tooltip
                title={d.label || (d.userType === "assignee" && userSelectData?.filter(u => u.value === d.objectId)?.[0]?.label)}
                placement="bottom"
              >
                <Avatar
                  // color={color[index] as AvatarColor}
                  className=" relative"
                  // size="extra-small"
                  style={{ margin: 4 }}
                  alt="User"
                >
                  {d?.label?.substring(0, 1)}
                  {(d.label === undefined || d.label === null) &&
                    d.userType === "assignee" &&
                    userSelectData &&
                    userSelectData.filter(data => data.value === d.objectId)?.[0]?.label?.substring(0, 1)}
                  {(d.label === undefined || d.label === null) &&
                    d.userType === "dept" &&
                    deptSelectData &&
                    deptSelectData.filter(data => data.value === d.objectId)?.[0]?.label?.substring(0, 1)}
                </Avatar>
              </Tooltip>
              {read !== true && (
                <i
                  onClick={() => {
                    onDataChange?.(value.filter(i => i.objectId !== d.objectId));
                  }}
                  className="absolute cursor-pointer hover:text-red-400 -top-2 right-0 hidden group-hover:block  icon-closeelement-bg-circle"
                />
              )}
            </div>
          );
        })}
        {read !== true && (multiple || modalSelected.length === 0) && (
          // <i
          //   onClick={() => setModalState(true)}
          //   className=" cursor-pointer hover:text-orange-400 text-2xl icon-task-add-member-circle"
          // />
          <i onClick={() => setModalState(true)}>
            <Avatar alt="PlusOutlined" style={{ margin: 4, backgroundColor: "#1677ff" }}>
              <Icon name="PlusOutlined" className="icon cursor-pointer" />
            </Avatar>
          </i>
        )}
      </div>
    </div>
  );
};
