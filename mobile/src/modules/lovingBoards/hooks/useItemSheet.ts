import { useCallback, useReducer, useState } from 'react';
import { Alert } from 'react-native';
import { useTranslation } from 'react-i18next';
import type { Board, Item } from '../types/api';
import type {
  CreateItemPayload,
  UpdateItemPayload,
} from '../services/itemService';

interface FormState {
  title: string;
  quantity: string;
  category: string;
  note: string;
  assigneeUserId: string;
  dueDate: string;
  isRecurring: boolean;
  saving: boolean;
}

type FormAction =
  | { type: 'SET_FIELD'; field: keyof FormState; value: string | boolean }
  | { type: 'SET_SAVING'; value: boolean };

function formReducer(state: FormState, action: FormAction): FormState {
  switch (action.type) {
    case 'SET_FIELD':
      return { ...state, [action.field]: action.value };
    case 'SET_SAVING':
      return { ...state, saving: action.value };
    default:
      return state;
  }
}

export interface UseItemSheetArgs {
  existingItem: Item | undefined;
  board: Board | null;
  isEditing: boolean;
  itemId: number | undefined;
  addItem: (payload: CreateItemPayload) => Promise<number | undefined>;
  updateItem: (itemId: number, payload: UpdateItemPayload) => Promise<void>;
  deleteItem: (itemId: number) => Promise<void>;
  onDismiss: () => void;
}

export interface UseItemSheetResult {
  form: FormState;
  titleError: string | undefined;
  dueDateError: string | undefined;
  memberIds: number[];
  setField: (field: keyof FormState, value: string | boolean) => void;
  handleSave: () => Promise<void>;
  handleDelete: () => void;
}

const useItemSheet = (args: UseItemSheetArgs): UseItemSheetResult => {
  const {
    existingItem,
    board,
    isEditing,
    itemId,
    addItem,
    updateItem,
    deleteItem: deleteItemFn,
    onDismiss,
  } = args;
  const { t } = useTranslation();

  const initialState: FormState = {
    title: existingItem?.title ?? '',
    quantity: existingItem?.quantity ?? '',
    category: existingItem?.category ?? '',
    note: existingItem?.note ?? '',
    assigneeUserId: existingItem?.assigneeUserId?.toString() ?? '',
    dueDate: existingItem?.expiryDate?.split('T')[0] ?? '',
    isRecurring: existingItem?.isRecurring ?? false,
    saving: false,
  };

  const [form, dispatch] = useReducer(formReducer, initialState);
  const [titleError, setTitleError] = useState<string | undefined>();
  const [dueDateError, setDueDateError] = useState<string | undefined>();

  const setField = useCallback(
    (field: keyof FormState, value: string | boolean) => {
      dispatch({ type: 'SET_FIELD', field, value });
      if (field === 'title' && typeof value === 'string' && value.trim()) {
        setTitleError(undefined);
      }
      if (field === 'dueDate') {
        setDueDateError(undefined);
      }
    },
    [],
  );

  const validateDueDate = useCallback((): boolean => {
    const raw = form.dueDate.trim();
    if (!raw) return true;
    const dateRegex = /^\d{4}-\d{2}-\d{2}$/;
    if (!dateRegex.test(raw) || Number.isNaN(new Date(raw).getTime())) {
      setDueDateError(t('lovingBoards.itemSheet.dueDateInvalid'));
      return false;
    }
    return true;
  }, [form.dueDate, t]);

  const handleSave = useCallback(async () => {
    const trimmedTitle = form.title.trim();
    if (!trimmedTitle) {
      setTitleError(t('lovingBoards.itemSheet.titleRequired'));
      return;
    }

    if (!validateDueDate()) return;

    dispatch({ type: 'SET_SAVING', value: true });
    try {
      if (isEditing && itemId !== undefined && existingItem) {
        // Edit mode — diff payload with only changed fields
        const diff: UpdateItemPayload = {};

        if (trimmedTitle !== existingItem.title) {
          diff.title = trimmedTitle;
        }

        const normalizedQuantity = (form.quantity.trim() || null) as
          string | null;
        const existingQuantity = (existingItem.quantity || null) as
          string | null;
        if (normalizedQuantity !== existingQuantity) {
          diff.quantity = normalizedQuantity;
        }

        const normalizedCategory = (form.category.trim() || null) as
          string | null;
        const existingCategory = (existingItem.category || null) as
          string | null;
        if (normalizedCategory !== existingCategory) {
          diff.category = normalizedCategory;
        }

        const normalizedNote = (form.note.trim() || null) as string | null;
        const existingNote = (existingItem.note || null) as string | null;
        if (normalizedNote !== existingNote) {
          diff.note = normalizedNote;
        }

        const normalizedAssignee: number | null = form.assigneeUserId.trim()
          ? Number(form.assigneeUserId.trim())
          : null;
        if (normalizedAssignee !== (existingItem.assigneeUserId ?? null)) {
          diff.assigneeUserId = normalizedAssignee;
        }

        const normalizedDate: string | null = form.dueDate.trim() || null;
        const existingDate: string | null =
          existingItem.expiryDate?.split('T')[0] ?? null;
        if (normalizedDate !== existingDate) {
          diff.expiryDate = normalizedDate;
        }

        if (form.isRecurring !== existingItem.isRecurring) {
          diff.isRecurring = form.isRecurring;
        }

        if (Object.keys(diff).length > 0) {
          await updateItem(itemId, diff);
        }
      } else {
        // Create mode — full payload
        const payload: CreateItemPayload = {
          title: trimmedTitle,
          quantity: form.quantity.trim() || null,
          category: form.category.trim() || null,
          note: form.note.trim() || null,
          assigneeUserId: form.assigneeUserId.trim()
            ? Number(form.assigneeUserId.trim())
            : null,
          expiryDate: form.dueDate.trim() || null,
          isRecurring: form.isRecurring,
        };
        await addItem(payload);
      }
      onDismiss();
    } catch {
      // error handled by hook
    } finally {
      dispatch({ type: 'SET_SAVING', value: false });
    }
  }, [
    form,
    isEditing,
    itemId,
    existingItem,
    addItem,
    updateItem,
    onDismiss,
    t,
    validateDueDate,
  ]);

  const handleDelete = useCallback(() => {
    if (!isEditing || itemId === undefined) return;
    Alert.alert(
      t('lovingBoards.itemSheet.delete'),
      t('lovingBoards.itemSheet.deleteConfirm'),
      [
        { text: t('common.cancel'), style: 'cancel' },
        {
          text: t('lovingBoards.itemSheet.delete'),
          style: 'destructive',
          onPress: async () => {
            try {
              await deleteItemFn(itemId);
              onDismiss();
            } catch {
              // error handled by hook
            }
          },
        },
      ],
    );
  }, [isEditing, itemId, deleteItemFn, onDismiss, t]);

  const memberIds = board?.memberUserIds ?? [];

  return {
    form,
    titleError,
    dueDateError,
    memberIds,
    setField,
    handleSave,
    handleDelete,
  };
};

export default useItemSheet;
