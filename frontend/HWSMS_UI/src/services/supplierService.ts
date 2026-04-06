import axios from "axios";
import { getAuthHeader } from "./authService";
import { API_BASE_URL } from "../config/api";

export type SupplierPayload = {
  name: string;
  contactInfo?: string;
};

export type Supplier = {
  id: number;
  name: string;
  contactInfo?: string;
  createdAt?: string;
};

const SUPPLIERS_API_URL = `${API_BASE_URL}/api/suppliers`;

export const addSupplier = async (payload: SupplierPayload) => {
  const { data } = await axios.post(SUPPLIERS_API_URL, payload, {
    headers: getAuthHeader(),
  });

  return data;
};

export const getSuppliers = async () => {
  const { data } = await axios.get<Supplier[]>(SUPPLIERS_API_URL, {
    headers: getAuthHeader(),
  });

  return data;
};

export const updateSupplier = async (id: number, payload: SupplierPayload) => {
  const { data } = await axios.put<string>(`${SUPPLIERS_API_URL}/${id}`, payload, {
    headers: getAuthHeader(),
  });

  return data;
};

export const deleteSupplier = async (id: number) => {
  const { data } = await axios.delete<string>(`${SUPPLIERS_API_URL}/${id}`, {
    headers: getAuthHeader(),
  });

  return data;
};
