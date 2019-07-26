import { MenuItem, createItem } from "../basic";
import { TEXT } from "@/common/lang";
import { MenuActions } from "@/events/FloatMenuClickEvent";
import context from "@/common/context";
import { isDynamicContent, getCleanParent, isDirty } from "@/kooboo/utils";
import { isLink } from "@/dom/utils";
import { getViewComment, getUrlComment, updateDomLink, updateUrlLink, updateAttributeLink, getAttributeComment, getRepeatComment } from "../utils";
import { KoobooComment } from "@/kooboo/KoobooComment";

export function createEditLinkItem(): MenuItem {
  const { el, setVisiable } = createItem(TEXT.EDIT_LINK, MenuActions.editLink);

  const update = (comments: KoobooComment[]) => {
    setVisiable(true);
    let args = context.lastSelectedDomEventArgs;
    if (getAttributeComment(comments)) return setVisiable(false);
    if (getRepeatComment(comments)) return setVisiable(false);
    if (getUrlComment(comments)) return setVisiable(true);
    if (!isLink(args.element)) return setVisiable(false);
    if (!getViewComment(comments)) return setVisiable(false);
    if (isDynamicContent(args.element)) return setVisiable(false);
  };

  el.addEventListener("click", async () => {
    let args = context.lastSelectedDomEventArgs;
    let comments = KoobooComment.getComments(args.element);
    let urlComment = getUrlComment(comments);
    let viewComment = getViewComment(comments)!;
    let { koobooId, parent } = getCleanParent(args.element);
    if (isDirty(args.element) && parent) {
      updateDomLink(parent, koobooId!, args.element, viewComment);
    } else if (urlComment) {
      updateUrlLink(args.element, args.koobooId!, urlComment, viewComment);
    } else {
      updateAttributeLink(args.element, args.koobooId!, viewComment);
    }
  });

  return { el, update };
}
