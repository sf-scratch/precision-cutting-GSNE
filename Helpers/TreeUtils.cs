using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using 精密切割系统.database.db.modle;
using 精密切割系统.View.F3_ModelCatalog;

namespace 精密切割系统.Utils
{
    internal class TreeUtils
    {
        public static ObservableCollection<FileTableModel> recursionMethod(List<FileTableModel> treeList) {
            ObservableCollection<FileTableModel> trees = new ObservableCollection<FileTableModel>();
            for (int i =0;i< treeList.Count();i++)
            {
                // 找出父节点
                if (0 == treeList[i].ParentId)
                {
                    // 调用递归方法填充子节点列表
                    trees.Add(findChildren(treeList[i], treeList));
                }
            }
            return trees;
        }

        /**
     * 递归方法
     * @param tree 父节点对象
     * @param treeList 所有的List
     * @return
     */
        public static FileTableModel findChildren(FileTableModel tree, List<FileTableModel> treeList)
        {

            for (int i = 0; i < treeList.Count(); i++) {
                if (tree.Id== treeList[i].ParentId)
                {
                    if (tree.Children == null)
                    {
                        tree.Children = new ObservableCollection<FileTableModel>();
                    }
                    tree.Children.Add(findChildren(treeList[i], treeList));
                }
            }
            return tree;
        }
    }


    //public static List<FileTableModel> recursionTreeNode(List<FileTableModel> treeList, ObservableCollection<TreeNode> ColTree)
    //{
    //    List<FileTableModel> trees = new List<FileTableModel>();
    //    for (int i = 0; i < treeList.Count(); i++)
    //    {
    //        // 找出父节点
    //        if (0 == treeList[i].ParentId)
    //        {
    //            // 调用递归方法填充子节点列表
    //            trees.Add(findChildren(treeList[i], treeList));
    //        }
    //    }
    //    return trees;
    //}

    //public class TreeNode
    //{
    //    public string Name { get; set; }
    //    public ObservableCollection<TreeNode> Children { get; set; }

    //    public TreeNode()
    //    {
    //        Children = new ObservableCollection<TreeNode>();
    //    }
    //}
}
