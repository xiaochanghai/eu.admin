import styled from "styled-components";

export const FormSetDiv = styled.div`
  height: 100vh;
  background: #ffffff;
  .fieldSet-header {
    padding: 16px;
    text-align: right;
    border-bottom: 1px solid #eee;
  }
  .fieldSet-main {
    padding: 16px 0px;
    height: calc(100% - 65px);
    box-sizing: border-box;
    .main-field {
      padding: 4px;
      margin-top: 8px;
      margin-bottom: 8px;
    }
    .ant-form-item {
      margin-bottom: 0px;
    }
    .ant-card-head-wrapper {
      .ant-card-head-title span {
        border-left: 2px solid var(--hooks-colorPrimary);
        padding-left: 5px;
      }
    }
    .main-hide-field-tag {
      height: 30px;
      font-size: 14px;
      line-height: 30px;
      border-color: var(--hooks-colorPrimary);
      position: relative;
      .plus {
        position: absolute;
        right: -8px;
        top: -14px;
        background: #fff;
        height: 18px;
        .icon {
          font-size: 16px;
          color: var(--hooks-colorPrimary);
        }
      }
    }
    &-content {
      height: 100%;
      &-left {
        height: 100%;
        overflow: auto;
        padding: 0px 16px 16px;
        .title {
          font-size: 16px;
          margin-bottom: 12px;
        }
        .sort-item {
          padding: 10px;
          border: 1px solid #eee;
          text-align: center;
          margin-bottom: 12px;
          border: 0.5px solid rgba(206, 210, 216, 1);
          box-shadow: 0px 2px 4px 0px rgba(49, 74, 111, 0.07);
          border-radius: 8px;
          font-size: 14px;
          color: #191c1f;
          cursor: move;
          &:hover {
            background: #e6f1fe;
            border: 0.5px solid rgba(27, 132, 251, 1);
            box-shadow: 0px 2px 4px 0px rgba(49, 74, 111, 0.08);
            color: #0077fa;
          }
        }
      }
      &-center {
        padding: 8px 8px 15px;
        background-color: #f3f6fc;
        height: 100%;
        overflow: auto;
        box-sizing: border-box;
        .field-center {
          height: 100%;
          overflow: auto;
          background-color: #fff;
          padding: 15px 12px;
          box-sizing: border-box;
          .main-tooltip {
            height: 100%;
            display: flex;
            justify-content: center;
            align-items: center;
            font-size: 14px;
            color: #989898;
          }
          .sortable-chosen,
          .sortable-ghost,
          .sortable-drag {
            cursor: move;
          }
          .main-field {
            cursor: move;
            position: relative;
            padding: 16px 20px;
            box-sizing: border-box;
            border: 1px solid transparent;
            border-radius: 8px;
            margin-bottom: 8px;
            &:hover {
              background: rgba(255, 255, 255, 0.44);
              border: 1px dashed rgba(10, 124, 251, 1);
              .main-field-delete {
                display: block;
              }
            }
            &-delete {
              font-size: 18px;
              position: absolute;
              top: 2px;
              right: 2px;
              color: #448aff;
              cursor: pointer;
              display: none;
            }
            .field-item {
              display: flex;
              align-items: center;
              .field-label {
                margin-right: 10px;
                width: 160px;
                text-align: right;
              }
              .field-form {
                flex: 1;
              }
            }
          }
        }
      }
      &-right {
        padding: 0px 16px;
        .title {
          font-size: 16px;
          margin-bottom: 12px;
        }
        .content {
          .label {
            margin-bottom: 10px;
          }
        }
      }
    }
  }
`;
