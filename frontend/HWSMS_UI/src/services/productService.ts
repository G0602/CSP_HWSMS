import axios from "axios";

export type ProductPayload = {
  name: string;
  sku: string;
  price: number;
  quantity: number;
  category: string;
};

export type Product = ProductPayload & {
  id: number;
  createdAt?: string;
};

const API_URL = import.meta.env.VITE_API_URL || "https://hsmsbackend-e9acfpeff8bycuax.indonesiacentral-01.azurewebsites.net/api/Product";

export const getProducts = async () => {
  return await axios.get<Product[]>(API_URL);
};

export const addProduct = async (product: ProductPayload) => {
  return await axios.post(API_URL, product);
};

export const updateProduct = async (id: number, product: ProductPayload) => {
  return await axios.put(`${API_URL}/${id}`, product);
};

export const deleteProduct = async (id: number) => {
  return await axios.delete(`${API_URL}/${id}`);
};
